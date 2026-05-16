namespace Backend.DTOs;

// --- What the user submits after reviewing/editing the AI extraction ---
public class QuotationCreateDto
{
    public string ClientName { get; set; } = string.Empty;
    public string? ClientEmail { get; set; }
    public string? ClientPhone { get; set; }
    public List<LineItemDto> LineItems { get; set; } = new();
    public decimal GstPercentage { get; set; }
    public int DeliveryDays { get; set; }
    public string? Notes { get; set; }
    public string OriginalPrompt { get; set; } = string.Empty;
}

public class LineItemDto
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
}

// --- Response sent back to the frontend ---
public class QuotationResponse
{
    public int Id { get; set; }
    public int? ClientId { get; set; }
    public string QuotationNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string? ClientEmail { get; set; }
    public string? ClientPhone { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal GstPercentage { get; set; }
    public decimal GstAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public int DeliveryDays { get; set; }
    public string ExpectedDeliveryDate { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<LineItemResponse> LineItems { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class LineItemResponse
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}
