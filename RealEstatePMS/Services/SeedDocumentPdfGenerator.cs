using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace RealEstatePMS.Services;

/// <summary>
/// Generates small, real PDF files used only for seeding sample Document records
/// (kept separate from Models.Entities so QuestPDF's own "Document" type doesn't collide
/// with the RealEstatePMS Document entity).
/// </summary>
public static class SeedDocumentPdfGenerator
{
    public static byte[] Generate(string title, string[] bodyLines)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text("RealEstate PMS").FontSize(16).Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().Text(title).FontSize(13).SemiBold();
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(15).Column(col =>
                {
                    foreach (var line in bodyLines)
                    {
                        col.Item().PaddingBottom(6).Text(line);
                    }
                });

                page.Footer().AlignCenter().Text($"Generated on {DateTime.Now:yyyy-MM-dd}").FontSize(8).FontColor(Colors.Grey.Medium);
            });
        });

        return document.GeneratePdf();
    }
}
