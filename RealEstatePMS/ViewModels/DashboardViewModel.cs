namespace RealEstatePMS.ViewModels;

public class DashboardViewModel
{
    public int TotalProjects { get; set; }
    public int ActiveProjects { get; set; }
    public int CompletedProjects { get; set; }

    public int TotalProperties { get; set; }
    public int AvailableProperties { get; set; }
    public int SoldProperties { get; set; }
    public int RentedProperties { get; set; }

    public int TotalCustomers { get; set; }

    public decimal TotalSales { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal PendingPayments { get; set; }

    public List<string> ChartMonths { get; set; } = new();
    public List<decimal> ChartSales { get; set; } = new();
    public List<decimal> ChartRevenue { get; set; } = new();

    public Dictionary<string, int> PropertyTypeDistribution { get; set; } = new();

    public List<RecentActivityItem> RecentActivities { get; set; } = new();
}

public class RecentActivityItem
{
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Icon { get; set; } = "bi-info-circle";
    public string ColorClass { get; set; } = "text-primary";
}
