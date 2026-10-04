namespace RealEstatePMS.Services;

public interface IExportService
{
    byte[] ExportToExcel(string sheetName, string[] headers, IEnumerable<object?[]> rows);
    byte[] ExportToPdf(string title, string[] headers, IEnumerable<object?[]> rows, string? subtitle = null);
}
