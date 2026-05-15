using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Backend.DTOs;

namespace Backend.Services;

public interface IGroqService
{
    Task<AiExtractionResult> ExtractQuotationDataAsync(string prompt);
}

public class GroqService : IGroqService
{
    private readonly HttpClient _httpClient;
    private readonly IKeyRotationService _keyRotationService;
    private readonly ILogger<GroqService> _logger;
    private static readonly ConcurrentDictionary<string, AiExtractionResult> _cache = new();
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

        // Check cache first to avoid unnecessary API calls
        var cacheKey = GetCacheKey(prompt);
        if (_cache.TryGetValue(cacheKey, out var cachedResult))
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

        var requestBody = new
        {
            model = "llama-3.3-70b-versatile",
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = systemInstruction
                },
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            temperature = 0.1,
            max_tokens = 1024
        };

        var url = "https://api.groq.com/openai/v1/chat/completions";
        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Add authorization header
        var apiKey = _keyRotationService.GetNextGroqKey();
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        _logger.LogInformation("Sending prompt to Groq for extraction...");
        
        var response = await _httpClient.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Groq API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
            throw new InvalidOperationException($"Groq API request failed: {response.StatusCode} - {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Groq response: {Response}", responseContent.Substring(0, Math.Min(500, responseContent.Length)));
        
        var jsonDocument = JsonDocument.Parse(responseContent);
        var root = jsonDocument.RootElement;

        if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            throw new InvalidOperationException("No choices in Groq response");

        var choice = choices[0];
        if (!choice.TryGetProperty("message", out var messageElement) || 
            !messageElement.TryGetProperty("content", out var textElement))
            throw new InvalidOperationException("No content in Groq response");

        var textContent = textElement.GetString();
        if (string.IsNullOrEmpty(textContent))
            throw new InvalidOperationException("Empty text content from Groq");

        // Clean up the text content - remove markdown code blocks if present
        textContent = textContent.Trim();
        if (textContent.StartsWith("```json"))
            textContent = textContent.Substring(7);
        if (textContent.StartsWith("```"))
            textContent = textContent.Substring(3);
        if (textContent.EndsWith("```"))
            textContent = textContent.Substring(0, textContent.Length - 3);
        textContent = textContent.Trim();

        var extractedData = JsonSerializer.Deserialize<AiExtractionResult>(textContent) 
            ?? throw new InvalidOperationException("Failed to deserialize Groq response");

        // Cache the result
        _cache.TryAdd(cacheKey, extractedData);
        _logger.LogInformation("API call successful. Result cached for {CacheTTL} hours", _cacheTTL.TotalHours);

        return extractedData;
    }

    private static string GetCacheKey(string prompt)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(prompt));
        return Convert.ToBase64String(hash);
    }
}
