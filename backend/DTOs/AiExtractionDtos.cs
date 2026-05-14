namespace Backend.DTOs;

// --- Request to Gemini ---
public class AiExtractionRequest
{
    public string Prompt { get; set; } = string.Empty;
}

// --- Structured response from Gemini ---
public class AiExtractionResult
{
    public string Intent { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string? ClientEmail { get; set; }
    public string? ClientPhone { get; set; }
    public List<ExtractedLineItem> LineItems { get; set; } = new();
    public decimal GstPercentage { get; set; }
    public int DeliveryDays { get; set; }
    public string? Notes { get; set; }
    public decimal Confidence { get; set; }
}

public class ExtractedLineItem
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
}
