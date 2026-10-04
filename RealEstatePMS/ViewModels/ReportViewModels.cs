namespace RealEstatePMS.ViewModels;

public class ReportFilterViewModel
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? ProjectId { get; set; }
    public int? PropertyId { get; set; }
    public int? CustomerId { get; set; }
    public string? SalesAgentId { get; set; }
}

public class ReportTableViewModel
{
    public string[] Headers { get; set; } = Array.Empty<string>();
    public List<object?[]> Rows { get; set; } = new();
    public string ReportKey { get; set; } = string.Empty;
}
