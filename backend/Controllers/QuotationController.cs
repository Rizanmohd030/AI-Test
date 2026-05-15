using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuotationController : ControllerBase
{
    private readonly IGroqService _groqService;
    private readonly IQuotationService _quotationService;
    private readonly IPdfService _pdfService;
    private readonly ILogger<QuotationController> _logger;

    public QuotationController(
        IGroqService groqService,
        IQuotationService quotationService,
        IPdfService pdfService,
        ILogger<QuotationController> logger)
    {
        _groqService = groqService;
        _quotationService = quotationService;
        _pdfService = pdfService;
        _logger = logger;
    }

    /// <summary>
    /// Step 1: AI Extraction — Send a natural language prompt to Groq.
    /// Returns structured data for the user to review/edit (human-in-the-loop).
    /// </summary>
    [HttpPost("extract")]
    public async Task<IActionResult> ExtractFromPrompt([FromBody] AiExtractionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest(new { error = "Prompt is required." });

        try
        {
            var result = await _groqService.ExtractQuotationDataAsync(request.Prompt);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI extraction failed for prompt: {Prompt}", request.Prompt);
            return StatusCode(500, new { error = "AI extraction failed. Please try again.", details = ex.Message });
        }
    }

    /// <summary>
    /// Step 2: Create Quotation — After user reviews/edits the extracted data,
    /// submit to create the quotation. Backend handles ALL business logic.
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> CreateQuotation([FromBody] QuotationCreateDto dto)
    {
        try
        {
            var quotation = await _quotationService.CreateQuotationAsync(dto);
            return CreatedAtAction(nameof(GetQuotation), new { id = quotation.Id }, quotation);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create quotation");
            return StatusCode(500, new { error = "Failed to create quotation.", details = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific quotation by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetQuotation(int id)
    {
        var quotation = await _quotationService.GetQuotationByIdAsync(id);
        if (quotation == null)
            return NotFound(new { error = $"Quotation with ID {id} not found." });

        return Ok(quotation);
    }

    /// <summary>
    /// Get all quotations (newest first).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllQuotations()
    {
        var quotations = await _quotationService.GetAllQuotationsAsync();
        return Ok(quotations);
    }

    /// <summary>
    /// Update quotation status (Draft → Sent → Accepted/Rejected).
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] StatusUpdateRequest request)
    {
        if (!Enum.TryParse<QuotationStatus>(request.Status, true, out var status))
            return BadRequest(new { error = $"Invalid status. Valid values: {string.Join(", ", Enum.GetNames<QuotationStatus>())}" });

        var quotation = await _quotationService.UpdateStatusAsync(id, status);
        if (quotation == null)
            return NotFound(new { error = $"Quotation with ID {id} not found." });

        return Ok(quotation);
    }

    /// <summary>
    /// Download quotation as PDF.
    /// </summary>
    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> DownloadPdf(int id)
    {
        var quotation = await _quotationService.GetQuotationByIdAsync(id);
        if (quotation == null)
            return NotFound(new { error = $"Quotation with ID {id} not found." });

        try
        {
            var pdfBytes = _pdfService.GenerateQuotationPdf(quotation);
            return File(pdfBytes, "application/pdf", $"{quotation.QuotationNumber}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PDF generation failed for quotation {Id}", id);
            return StatusCode(500, new { error = "PDF generation failed.", details = ex.Message });
        }
    }
}

public class StatusUpdateRequest
{
    public string Status { get; set; } = string.Empty;
}
