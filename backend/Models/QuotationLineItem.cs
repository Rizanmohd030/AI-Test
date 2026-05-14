namespace Backend.Models;

public class QuotationLineItem
{
    public int Id { get; set; }

    public int QuotationId { get; set; }

    public string Description { get; set; } = string.Empty;

    public int Quantity { get; set; } = 1;

    /// <summary>Price per unit in INR</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Calculated: Quantity * UnitPrice</summary>
    public decimal Amount { get; set; }

    // Navigation property
    public Quotation Quotation { get; set; } = null!;
}
