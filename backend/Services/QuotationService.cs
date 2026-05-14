using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public interface IQuotationService
{
    Task<QuotationResponse> CreateQuotationAsync(QuotationCreateDto dto);
    Task<QuotationResponse?> GetQuotationByIdAsync(int id);
    Task<List<QuotationResponse>> GetAllQuotationsAsync();
    Task<QuotationResponse?> UpdateStatusAsync(int id, QuotationStatus status);
}

public class QuotationService : IQuotationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<QuotationService> _logger;

    public QuotationService(AppDbContext db, ILogger<QuotationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Creates a quotation — ALL business logic lives here, NOT in the AI layer.
    /// Handles: validation, quotation number generation, tax calculation, persistence.
    /// </summary>
    public async Task<QuotationResponse> CreateQuotationAsync(QuotationCreateDto dto)
    {
        // --- VALIDATION (backend is the authority) ---
        if (string.IsNullOrWhiteSpace(dto.ClientName))
            throw new ArgumentException("Client name is required.");

        if (dto.LineItems == null || dto.LineItems.Count == 0)
            throw new ArgumentException("At least one line item is required.");

        if (dto.GstPercentage < 0 || dto.GstPercentage > 100)
            throw new ArgumentException("GST percentage must be between 0 and 100.");

        if (dto.DeliveryDays <= 0)
            throw new ArgumentException("Delivery days must be greater than 0.");

        foreach (var item in dto.LineItems)
        {
            if (string.IsNullOrWhiteSpace(item.Description))
                throw new ArgumentException("Line item description is required.");
            if (item.Quantity <= 0)
                throw new ArgumentException("Line item quantity must be greater than 0.");
            if (item.UnitPrice < 0)
                throw new ArgumentException("Line item unit price cannot be negative.");
        }

        // --- QUOTATION NUMBER GENERATION ---
        var quotationNumber = await GenerateQuotationNumberAsync();

        // --- CALCULATIONS (backend is the authority) ---
        var lineItems = dto.LineItems.Select(li => new QuotationLineItem
        {
            Description = li.Description.Trim(),
            Quantity = li.Quantity,
            UnitPrice = li.UnitPrice,
            Amount = li.Quantity * li.UnitPrice // Backend calculates
        }).ToList();

        var subTotal = lineItems.Sum(li => li.Amount);
        var gstAmount = Math.Round(subTotal * dto.GstPercentage / 100, 2);
        var totalAmount = subTotal + gstAmount;
        var expectedDeliveryDate = DateOnly.FromDateTime(DateTime.Now.AddDays(dto.DeliveryDays));

        // --- PERSISTENCE ---
        var quotation = new Quotation
        {
            QuotationNumber = quotationNumber,
            ClientName = dto.ClientName.Trim(),
            ClientEmail = dto.ClientEmail?.Trim(),
            ClientPhone = dto.ClientPhone?.Trim(),
            Status = QuotationStatus.Draft,
            SubTotal = subTotal,
            GstPercentage = dto.GstPercentage,
            GstAmount = gstAmount,
            TotalAmount = totalAmount,
            DeliveryDays = dto.DeliveryDays,
            ExpectedDeliveryDate = expectedDeliveryDate,
            Notes = dto.Notes?.Trim(),
            OriginalPrompt = dto.OriginalPrompt,
            LineItems = lineItems,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Quotations.Add(quotation);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created quotation {QuotationNumber} for {Client} — Total: ₹{Total}",
            quotationNumber, quotation.ClientName, totalAmount);

        return MapToResponse(quotation);
    }

    public async Task<QuotationResponse?> GetQuotationByIdAsync(int id)
    {
        var quotation = await _db.Quotations
            .Include(q => q.LineItems)
            .FirstOrDefaultAsync(q => q.Id == id);

        return quotation == null ? null : MapToResponse(quotation);
    }

    public async Task<List<QuotationResponse>> GetAllQuotationsAsync()
    {
        var quotations = await _db.Quotations
            .Include(q => q.LineItems)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        return quotations.Select(MapToResponse).ToList();
    }

    public async Task<QuotationResponse?> UpdateStatusAsync(int id, QuotationStatus status)
    {
        var quotation = await _db.Quotations
            .Include(q => q.LineItems)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quotation == null) return null;

        quotation.Status = status;
        quotation.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated quotation {QuotationNumber} status to {Status}",
            quotation.QuotationNumber, status);

        return MapToResponse(quotation);
    }

    // --- PRIVATE HELPERS ---

    private async Task<string> GenerateQuotationNumberAsync()
    {
        var year = DateTime.Now.Year;
        var prefix = $"QTN-{year}-";

        var lastQuotation = await _db.Quotations
            .Where(q => q.QuotationNumber.StartsWith(prefix))
            .OrderByDescending(q => q.QuotationNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastQuotation != null)
        {
            var lastNumberStr = lastQuotation.QuotationNumber.Replace(prefix, "");
            if (int.TryParse(lastNumberStr, out var lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}{nextNumber:D4}"; // e.g., QTN-2026-0001
    }

    private static QuotationResponse MapToResponse(Quotation q) => new()
    {
        Id = q.Id,
        QuotationNumber = q.QuotationNumber,
        ClientName = q.ClientName,
        ClientEmail = q.ClientEmail,
        ClientPhone = q.ClientPhone,
        Status = q.Status.ToString(),
        SubTotal = q.SubTotal,
        GstPercentage = q.GstPercentage,
        GstAmount = q.GstAmount,
        TotalAmount = q.TotalAmount,
        DeliveryDays = q.DeliveryDays,
        ExpectedDeliveryDate = q.ExpectedDeliveryDate.ToString("yyyy-MM-dd"),
        Notes = q.Notes,
        LineItems = q.LineItems.Select(li => new LineItemResponse
        {
            Id = li.Id,
            Description = li.Description,
            Quantity = li.Quantity,
            UnitPrice = li.UnitPrice,
            Amount = li.Amount
        }).ToList(),
        CreatedAt = q.CreatedAt
    };
}
