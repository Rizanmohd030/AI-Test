using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Backend.DTOs;

namespace Backend.Services;

public interface IPdfService
{
    byte[] GenerateQuotationPdf(QuotationResponse quotation);
}

public class PdfService : IPdfService
{
    public byte[] GenerateQuotationPdf(QuotationResponse quotation)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));

                // --- HEADER ---
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("QUOTATION")
                                .FontSize(28).Bold().FontColor(Colors.Blue.Darken2);
                            c.Item().Text(quotation.QuotationNumber)
                                .FontSize(14).FontColor(Colors.Grey.Darken1);
                        });

                        row.ConstantItem(150).AlignRight().Column(c =>
                        {
                            c.Item().Text("Your Company Name").Bold().FontSize(12);
                            c.Item().Text("your@email.com").FontSize(9).FontColor(Colors.Grey.Medium);
                            c.Item().Text("+91 XXXXX XXXXX").FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                    });

                    col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                // --- CONTENT ---
                page.Content().Column(col =>
                {
                    // Client info
                    col.Item().PaddingBottom(15).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Bill To:").Bold().FontColor(Colors.Grey.Darken1);
                            c.Item().Text(quotation.ClientName).Bold().FontSize(14);
                            if (!string.IsNullOrEmpty(quotation.ClientEmail))
                                c.Item().Text(quotation.ClientEmail).FontSize(10).FontColor(Colors.Grey.Medium);
                            if (!string.IsNullOrEmpty(quotation.ClientPhone))
                                c.Item().Text(quotation.ClientPhone).FontSize(10).FontColor(Colors.Grey.Medium);
                        });

                        row.ConstantItem(200).AlignRight().Column(c =>
                        {
                            c.Item().Text($"Date: {quotation.CreatedAt:dd MMM yyyy}").FontSize(10);
                            c.Item().Text($"Status: {quotation.Status}").FontSize(10).Bold();
                            c.Item().Text($"Delivery: {quotation.ExpectedDeliveryDate}").FontSize(10);
                        });
                    });

                    // Line items table
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);   // #
                            columns.RelativeColumn(4);     // Description
                            columns.ConstantColumn(60);    // Qty
                            columns.ConstantColumn(100);   // Unit Price
                            columns.ConstantColumn(100);   // Amount
                        });

                        // Table header
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                .Text("#").FontColor(Colors.White).Bold().FontSize(10);
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                .Text("Description").FontColor(Colors.White).Bold().FontSize(10);
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight()
                                .Text("Qty").FontColor(Colors.White).Bold().FontSize(10);
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight()
                                .Text("Unit Price (₹)").FontColor(Colors.White).Bold().FontSize(10);
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight()
                                .Text("Amount (₹)").FontColor(Colors.White).Bold().FontSize(10);
                        });

                        // Table rows
                        for (int i = 0; i < quotation.LineItems.Count; i++)
                        {
                            var item = quotation.LineItems[i];
                            var bgColor = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                            table.Cell().Background(bgColor).Padding(5)
                                .Text($"{i + 1}").FontSize(10);
                            table.Cell().Background(bgColor).Padding(5)
                                .Text(item.Description).FontSize(10);
                            table.Cell().Background(bgColor).Padding(5).AlignRight()
                                .Text($"{item.Quantity}").FontSize(10);
                            table.Cell().Background(bgColor).Padding(5).AlignRight()
                                .Text($"{item.UnitPrice:N2}").FontSize(10);
                            table.Cell().Background(bgColor).Padding(5).AlignRight()
                                .Text($"{item.Amount:N2}").FontSize(10);
                        }
                    });

                    // Totals
                    col.Item().PaddingTop(10).AlignRight().Width(250).Column(totals =>
                    {
                        totals.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Sub Total:").AlignRight();
                            row.ConstantItem(100).Text($"₹{quotation.SubTotal:N2}").AlignRight();
                        });

                        totals.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"GST ({quotation.GstPercentage}%):").AlignRight();
                            row.ConstantItem(100).Text($"₹{quotation.GstAmount:N2}").AlignRight();
                        });

                        totals.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        totals.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text("Total:").Bold().FontSize(14).AlignRight();
                            row.ConstantItem(100).Text($"₹{quotation.TotalAmount:N2}")
                                .Bold().FontSize(14).FontColor(Colors.Blue.Darken2).AlignRight();
                        });
                    });

                    // Notes
                    if (!string.IsNullOrEmpty(quotation.Notes))
                    {
                        col.Item().PaddingTop(25).Column(notes =>
                        {
                            notes.Item().Text("Notes:").Bold().FontColor(Colors.Grey.Darken1);
                            notes.Item().PaddingTop(3).Text(quotation.Notes).FontSize(10).Italic();
                        });
                    }
                });

                // --- FOOTER ---
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Thank you for your business!").FontSize(9).FontColor(Colors.Grey.Medium);
                });
            });
        });

        return document.GeneratePdf();
    }
}
