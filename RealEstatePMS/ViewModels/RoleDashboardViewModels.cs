using RealEstatePMS.Models.Entities;

namespace RealEstatePMS.ViewModels;

public class ProjectManagerDashboardViewModel
{
    public int MyProjectsCount { get; set; }
    public int ActiveProjectsCount { get; set; }
    public int CompletedProjectsCount { get; set; }
    public int OnHoldProjectsCount { get; set; }

    public int MyTasksCount { get; set; }
    public int PendingTasksCount { get; set; }
    public int InProgressTasksCount { get; set; }
    public int DelayedTasksCount { get; set; }

    public List<Project> MyProjects { get; set; } = new();
    public List<ProjectTask> UpcomingTasks { get; set; } = new();
    public List<ProjectTask> DelayedTasks { get; set; } = new();
    public List<RecentActivityItem> RecentActivities { get; set; } = new();
}

public class SalesAgentDashboardViewModel
{
    public int MySalesCount { get; set; }
    public decimal MySalesTotal { get; set; }
    public int MyPendingBalanceCount { get; set; }
    public decimal MyPendingBalanceTotal { get; set; }
    public int MyCustomersCount { get; set; }
    public int AvailablePropertiesCount { get; set; }
    public int PendingBookingRequestsCount { get; set; }

    public List<Sale> RecentSales { get; set; } = new();
    public List<BookingRequest> PendingBookingRequests { get; set; } = new();

    public List<string> ChartMonths { get; set; } = new();
    public List<decimal> ChartMySales { get; set; } = new();
}

public class AccountantDashboardViewModel
{
    public decimal TotalRevenue { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public decimal PendingSalesBalance { get; set; }
    public decimal PendingRentalBalance { get; set; }
    public int OverdueSalesCount { get; set; }
    public int OverdueRentalsCount { get; set; }

    public List<Payment> RecentPayments { get; set; } = new();
    public List<Sale> OutstandingSales { get; set; } = new();

    public List<string> ChartMonths { get; set; } = new();
    public List<decimal> ChartRevenue { get; set; } = new();
}

public class CustomerDashboardViewModel
{
    public bool HasCustomerProfile { get; set; }

    public int ActiveSalesCount { get; set; }
    public int ActiveRentalsCount { get; set; }
    public int PendingBookingsCount { get; set; }

    public decimal TotalPaid { get; set; }
    public decimal TotalOutstanding { get; set; }

    public List<Sale> MySales { get; set; } = new();
    public List<Rental> MyRentals { get; set; } = new();
    public List<BookingRequest> MyBookings { get; set; } = new();
    public List<Payment> RecentPayments { get; set; } = new();
}
