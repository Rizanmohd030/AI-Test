using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Backend.DTOs;

namespace Backend.Services;

public interface IGroqService
{
    Task<AiExtractionResult> ExtractQuotationDataAsync(string prompt);
    Task<ClientCommandAnalysis> InterpretClientCommandAsync(string prompt);
}

public class GroqService : IGroqService
{
    private readonly HttpClient _httpClient;
    private readonly IKeyRotationService _keyRotationService;
    private readonly ILogger<GroqService> _logger;
    private static readonly ConcurrentDictionary<string, AiExtractionResult> _quotationCache = new();
    private static readonly ConcurrentDictionary<string, ClientCommandAnalysis> _clientCache = new();
    private static readonly TimeSpan _cacheTTL = TimeSpan.FromHours(1);

    public GroqService(HttpClient httpClient, IKeyRotationService keyRotationService, ILogger<GroqService> logger)
    {
        _httpClient = httpClient;
        _keyRotationService = keyRotationService;
        _logger = logger;
    }

    public async Task<AiExtractionResult> ExtractQuotationDataAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be empty");

        var cacheKey = GetCacheKey(prompt);
        if (_quotationCache.TryGetValue(cacheKey, out var cachedResult))
        {
            _logger.LogInformation("Cache hit for prompt (quota saved)");
            return cachedResult;
        }

        var systemInstruction = @"You are a structured data extraction assistant for an ERP quotation system.
Given a natural language input, extract the following fields and return ONLY valid JSON (no markdown, no explanation):

{
  ""intent"": ""create_quotation"",
  ""clientName"": ""<extracted client/company name>"",
  ""clientEmail"": ""<extracted email or null>"",
  ""clientPhone"": ""<extracted phone or null>"",
  ""lineItems"": [
    {
      ""description"": ""<service or product description>"",
      ""quantity"": <number, default 1>,
      ""unitPrice"": <number in INR>
    }
  ],
  ""gstPercentage"": <number like 18>,
  ""deliveryDays"": <number of days>,
  ""notes"": ""<any additional notes or null>"",
  ""confidence"": <0.0 to 1.0 indicating extraction confidence>
}

Rules:
- If a single total amount is mentioned without breakdown, create one line item with quantity=1 and unitPrice=that amount.
- If GST is not mentioned, default to 18.
- If delivery days are not mentioned, default to 30.
- Always return valid JSON. No markdown fences. No extra text.
- confidence should reflect how certain you are about the extraction (1.0 = very confident).";

        var responseContent = await SendGroqPromptAsync(systemInstruction, prompt);
        var textContent = ExtractMessageContent(responseContent);
        textContent = NormalizeJson(textContent);

        var extractedData = JsonSerializer.Deserialize<AiExtractionResult>(textContent) 
            ?? throw new InvalidOperationException("Failed to deserialize Groq response");

        _quotationCache.TryAdd(cacheKey, extractedData);
        _logger.LogInformation("API call successful. Result cached for {CacheTTL} hours", _cacheTTL.TotalHours);

        return extractedData;
    }

    public async Task<ClientCommandAnalysis> InterpretClientCommandAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be empty");

        var cacheKey = GetCacheKey($"client:{prompt}");
        if (_clientCache.TryGetValue(cacheKey, out var cachedResult))
        {
            _logger.LogInformation("Cache hit for client prompt (quota saved)");
            return cachedResult;
        }

        var systemInstruction = @"You are a strict JSON parser for client CRUD commands in an ERP system.
Return ONLY valid JSON with this shape:
{
  ""intent"": ""get_last_client|get_client|list_clients|create_client|update_client|delete_client|create_quotation"",
  ""clientId"": <number or null>,
  ""searchTerm"": ""<name/email/phone or null>"",
  ""name"": ""<client name or null>"",
  ""email"": ""<email or null>"",
  ""phone"": ""<phone or null>"",
  ""notes"": ""<notes or null>""
}

Rules:
- If the prompt asks for the last client created, use intent get_last_client.
- If the prompt asks to show/list all clients, use list_clients.
- If the prompt asks to create/add/register a client, use create_client.
- If the prompt asks to update/edit/modify a client, use update_client.
- If the prompt asks to delete/remove a client, use delete_client.
- If the prompt asks to find/search/fetch a client by name/email/phone/id, use get_client.
- If the prompt asks to create, generate, or make a quotation/invoice/estimate, use create_quotation.
- Prefer clientId when explicitly present; otherwise fill searchTerm.";

        var responseContent = await SendGroqPromptAsync(systemInstruction, prompt);
        var textContent = NormalizeJson(ExtractMessageContent(responseContent));

        var extracted = JsonSerializer.Deserialize<ClientCommandAnalysis>(textContent)
            ?? throw new InvalidOperationException("Failed to deserialize Groq client command");

        _clientCache.TryAdd(cacheKey, extracted);
        return extracted;
    }

    private async Task<string> SendGroqPromptAsync(string systemInstruction, string prompt)
    {
        var requestBody = new
        {
            model = "llama-3.3-70b-versatile",
            messages = new[]
            {
                new { role = "system", content = systemInstruction },
                new { role = "user", content = prompt }
            },
            temperature = 0.1,
            max_tokens = 1024,
            response_format = new { type = "json_object" }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _keyRotationService.GetNextGroqKey());

        _logger.LogInformation("Sending prompt to Groq...");
        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Groq API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
            throw new InvalidOperationException($"Groq API request failed: {response.StatusCode} - {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Groq response: {Response}", responseContent.Substring(0, Math.Min(500, responseContent.Length)));
        return responseContent;
    }

    private static string ExtractMessageContent(string responseContent)
    {
        var jsonDocument = JsonDocument.Parse(responseContent);
        var root = jsonDocument.RootElement;

        if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            throw new InvalidOperationException("No choices in Groq response");

        var choice = choices[0];
        if (!choice.TryGetProperty("message", out var messageElement) ||
            !messageElement.TryGetProperty("content", out var textElement))
            throw new InvalidOperationException("No content in Groq response");

        return textElement.GetString() ?? throw new InvalidOperationException("Empty text content from Groq");
    }

    private static string NormalizeJson(string textContent)
    {
        if (string.IsNullOrWhiteSpace(textContent)) return "{}";

        // Find the first '{' and the last '}'
        int start = textContent.IndexOf('{');
        int end = textContent.LastIndexOf('}');

        if (start != -1 && end != -1 && end > start)
        {
            return textContent.Substring(start, end - start + 1);
        }

        return textContent.Trim();
    }

    private static string GetCacheKey(string prompt)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(prompt));
        return Convert.ToBase64String(hash);
    }
}
