using System.Text;
using System.Text.Json;
using Backend.DTOs;

namespace Backend.Services;

public interface IGeminiService
{
    Task<AiExtractionResult> ExtractQuotationDataAsync(string prompt);
}

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<GeminiService> _logger;

    public GeminiService(HttpClient httpClient, IConfiguration config, ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _apiKey = config["Gemini:ApiKey"] ?? throw new InvalidOperationException("Gemini:ApiKey is not configured");
        _model = config["Gemini:Model"] ?? "gemini-2.0-flash";
        _logger = logger;
    }

    public async Task<AiExtractionResult> ExtractQuotationDataAsync(string prompt)
    {
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
            system_instruction = new
            {
                parts = new[] { new { text = systemInstruction } }
            },
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                temperature = 0.1,
                responseMimeType = "application/json"
            }
        };

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation("Sending prompt to Gemini for extraction (MOCKED)...");
        
        // Simulating network delay
        await Task.Delay(1000);

        // MOCKED RESPONSE
        return new AiExtractionResult
        {
            Intent = "create_quotation",
            ClientName = "Acme Corp",
            ClientEmail = "hello@acmecorp.com",
            ClientPhone = "+91-9876543210",
            GstPercentage = 18,
            DeliveryDays = 60,
            Notes = "This is a mocked AI extraction result because the API key had a limit of 0.",
            Confidence = 0.99m,
            LineItems = new List<ExtractedLineItem>
            {
                new ExtractedLineItem { Description = "New Mobile App", Quantity = 1, UnitPrice = 120000 },
                new ExtractedLineItem { Description = "Server Hosting (1 Year)", Quantity = 1, UnitPrice = 20000 }
            }
        };
    }
}
