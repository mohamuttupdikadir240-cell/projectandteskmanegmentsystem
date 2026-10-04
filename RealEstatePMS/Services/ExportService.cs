using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace RealEstatePMS.Services;

public class ExportService : IExportService
{
    public byte[] ExportToExcel(string sheetName, string[] headers, IEnumerable<object?[]> rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(string.IsNullOrWhiteSpace(sheetName) ? "Report" : sheetName);

        for (int c = 0; c < headers.Length; c++)
        {
            var cell = worksheet.Cell(1, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e3a5f");
            cell.Style.Font.FontColor = XLColor.White;
        }

        int rowIndex = 2;
        foreach (var row in rows)
        {
            for (int c = 0; c < row.Length; c++)
            {
                var value = row[c];
                var cell = worksheet.Cell(rowIndex, c + 1);
                if (value is decimal or double or int or long)
                    cell.Value = Convert.ToDouble(value);
                else if (value is DateTime dt)
                    cell.Value = dt;
                else
                    cell.Value = value?.ToString() ?? string.Empty;
            }
            rowIndex++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportToPdf(string title, string[] headers, IEnumerable<object?[]> rows, string? subtitle = null)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var rowList = rows.ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text(title).FontSize(18).Bold().FontColor(Colors.Blue.Darken3);
                    if (!string.IsNullOrWhiteSpace(subtitle))
                        col.Item().Text(subtitle).FontSize(10).FontColor(Colors.Grey.Darken1);
                    col.Item().Text($"Generated on {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        foreach (var _ in headers) columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        foreach (var h in headers)
                        {
                            header.Cell().Background(Colors.Blue.Darken3).Padding(5)
                                .Text(h).FontColor(Colors.White).Bold();
                        }
                    });

                    bool alternate = false;
                    foreach (var row in rowList)
                    {
                        var bg = alternate ? Colors.Grey.Lighten4 : Colors.White;
                        foreach (var cellValue in row)
                        {
                            table.Cell().Background(bg).Padding(5).Text(FormatValue(cellValue));
                        }
                        alternate = !alternate;
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            decimal d => d.ToString("N2"),
            DateTime dt => dt.ToString("yyyy-MM-dd"),
            _ => value.ToString() ?? string.Empty
        };
    }
}
