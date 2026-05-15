namespace Backend.DTOs;

// --- Request to Groq ---
public class AiExtractionRequest
{
    public string Prompt { get; set; } = string.Empty;
}

// --- Structured response from Groq ---
public class AiExtractionResult
{
    [System.Text.Json.Serialization.JsonPropertyName("intent")]
    public string Intent { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("clientName")]
    public string ClientName { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("clientEmail")]
    public string? ClientEmail { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("clientPhone")]
    public string? ClientPhone { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("lineItems")]
    public List<ExtractedLineItem> LineItems { get; set; } = new();
    
    [System.Text.Json.Serialization.JsonPropertyName("gstPercentage")]
    public decimal GstPercentage { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("deliveryDays")]
    public int DeliveryDays { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("notes")]
    public string? Notes { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("confidence")]
    public decimal Confidence { get; set; }
}

public class ExtractedLineItem
{
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("quantity")]
    public int Quantity { get; set; } = 1;
    
    [System.Text.Json.Serialization.JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }
}
