namespace Backend.Models;

public enum QuotationStatus
{
    Draft,
    Sent,
    Accepted,
    Rejected
}

public class Quotation
{
    public int Id { get; set; }

    public int? ClientId { get; set; }

    /// <summary>Auto-generated quotation number like QTN-2026-0001</summary>
    public string QuotationNumber { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;
    public string? ClientEmail { get; set; }
    public string? ClientPhone { get; set; }

    public QuotationStatus Status { get; set; } = QuotationStatus.Draft;

    /// <summary>Sum of all line item amounts (before tax)</summary>
    public decimal SubTotal { get; set; }

    public decimal GstPercentage { get; set; }

    /// <summary>Calculated: SubTotal * GstPercentage / 100</summary>
    public decimal GstAmount { get; set; }

    /// <summary>Calculated: SubTotal + GstAmount</summary>
    public decimal TotalAmount { get; set; }

    public int DeliveryDays { get; set; }
    public DateOnly ExpectedDeliveryDate { get; set; }

    public string? Notes { get; set; }

    /// <summary>The original natural language prompt from the user</summary>
    public string OriginalPrompt { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Client? Client { get; set; }

    // Navigation property
    public List<QuotationLineItem> LineItems { get; set; } = new();
}
