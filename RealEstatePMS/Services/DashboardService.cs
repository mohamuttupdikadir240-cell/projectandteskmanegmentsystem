using Microsoft.EntityFrameworkCore;
using RealEstatePMS.Data;
using RealEstatePMS.Models.Enums;
using RealEstatePMS.ViewModels;

namespace RealEstatePMS.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardViewModel> BuildAsync()
    {
        var vm = new DashboardViewModel
        {
            TotalProjects = await _context.Projects.CountAsync(),
            ActiveProjects = await _context.Projects.CountAsync(p => p.Status == ProjectStatus.InProgress),
            CompletedProjects = await _context.Projects.CountAsync(p => p.Status == ProjectStatus.Completed),

            TotalProperties = await _context.Properties.CountAsync(),
            AvailableProperties = await _context.Properties.CountAsync(p => p.Status == PropertyStatus.Available),
            SoldProperties = await _context.Properties.CountAsync(p => p.Status == PropertyStatus.Sold),
            RentedProperties = await _context.Properties.CountAsync(p => p.Status == PropertyStatus.Rented),

            TotalCustomers = await _context.Customers.CountAsync(),

            TotalSales = await _context.Sales.SumAsync(s => (decimal?)s.FinalPrice) ?? 0,
        };

        var salesPayments = await _context.Payments.SumAsync(p => (decimal?)p.Amount) ?? 0;
        vm.TotalRevenue = salesPayments;

        vm.PendingPayments = (await _context.Sales.Where(s => s.RemainingBalance > 0).SumAsync(s => (decimal?)s.RemainingBalance) ?? 0)
            + (await _context.Rentals.Where(r => r.PaymentStatus != PaymentStatus.Paid).SumAsync(r => (decimal?)r.MonthlyRent) ?? 0);

        // Last 8 months chart
        var now = DateTime.UtcNow;
        for (int i = 7; i >= 0; i--)
        {
            var month = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            var nextMonth = month.AddMonths(1);

            var salesInMonth = await _context.Sales
                .Where(s => s.SaleDate >= month && s.SaleDate < nextMonth)
                .SumAsync(s => (decimal?)s.FinalPrice) ?? 0;

            var revenueInMonth = await _context.Payments
                .Where(p => p.PaymentDate >= month && p.PaymentDate < nextMonth)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            vm.ChartMonths.Add(month.ToString("MMM"));
            vm.ChartSales.Add(salesInMonth);
            vm.ChartRevenue.Add(revenueInMonth);
        }

        vm.PropertyTypeDistribution = await _context.Properties
            .GroupBy(p => p.Type)
            .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count);

        var activities = new List<RecentActivityItem>();

        var recentSales = await _context.Sales.Include(s => s.Property).Include(s => s.Customer)
            .OrderByDescending(s => s.CreatedDate).Take(3).ToListAsync();
        activities.AddRange(recentSales.Select(s => new RecentActivityItem
        {
            Description = $"New sale added - {s.Property?.Code} to {s.Customer?.Name}",
            Date = s.CreatedDate,
            Icon = "bi-cash-coin",
            ColorClass = "text-success"
        }));

        var recentPayments = await _context.Payments.Include(p => p.Customer)
            .OrderByDescending(p => p.CreatedDate).Take(3).ToListAsync();
        activities.AddRange(recentPayments.Select(p => new RecentActivityItem
        {
            Description = $"Payment received - {p.Amount:C} from {p.Customer?.Name}",
            Date = p.CreatedDate,
            Icon = "bi-wallet2",
            ColorClass = "text-primary"
        }));

        var recentCustomers = await _context.Customers
            .OrderByDescending(c => c.RegistrationDate).Take(2).ToListAsync();
        activities.AddRange(recentCustomers.Select(c => new RecentActivityItem
        {
            Description = $"New customer registered - {c.Name}",
            Date = c.RegistrationDate,
            Icon = "bi-person-plus",
            ColorClass = "text-info"
        }));

        var recentTasks = await _context.ProjectTasks
            .OrderByDescending(t => t.CreatedDate).Take(2).ToListAsync();
        activities.AddRange(recentTasks.Select(t => new RecentActivityItem
        {
            Description = $"Task '{t.Name}' is {t.Status}",
            Date = t.CreatedDate,
            Icon = t.Status == ProjectTaskStatus.Delayed ? "bi-exclamation-triangle" : "bi-check2-square",
            ColorClass = t.Status == ProjectTaskStatus.Delayed ? "text-danger" : "text-warning"
        }));

        vm.RecentActivities = activities.OrderByDescending(a => a.Date).Take(8).ToList();

        return vm;
    }

    public async Task<ProjectManagerDashboardViewModel> BuildForProjectManagerAsync(string userId)
    {
        var myProjects = await _context.Projects
            .Include(p => p.Properties)
            .Where(p => p.ProjectManagerId == userId)
            .OrderByDescending(p => p.CreatedDate)
            .ToListAsync();

        var projectIds = myProjects.Select(p => p.Id).ToList();

        var tasks = await _context.ProjectTasks
            .Include(t => t.Project)
            .Include(t => t.AssignedEmployee)
            .Where(t => projectIds.Contains(t.ProjectId))
            .ToListAsync();

        var vm = new ProjectManagerDashboardViewModel
        {
            MyProjectsCount = myProjects.Count,
            ActiveProjectsCount = myProjects.Count(p => p.Status == ProjectStatus.InProgress),
            CompletedProjectsCount = myProjects.Count(p => p.Status == ProjectStatus.Completed),
            OnHoldProjectsCount = myProjects.Count(p => p.Status == ProjectStatus.OnHold),

            MyTasksCount = tasks.Count,
            PendingTasksCount = tasks.Count(t => t.Status == ProjectTaskStatus.Pending),
            InProgressTasksCount = tasks.Count(t => t.Status == ProjectTaskStatus.InProgress),
            DelayedTasksCount = tasks.Count(t => t.Status == ProjectTaskStatus.Delayed),

            MyProjects = myProjects,
            UpcomingTasks = tasks.Where(t => t.Status != ProjectTaskStatus.Completed)
                .OrderBy(t => t.DueDate).Take(6).ToList(),
            DelayedTasks = tasks.Where(t => t.Status == ProjectTaskStatus.Delayed)
                .OrderBy(t => t.DueDate).Take(6).ToList()
        };

        var activities = new List<RecentActivityItem>();
        activities.AddRange(tasks.OrderByDescending(t => t.CreatedDate).Take(5).Select(t => new RecentActivityItem
        {
            Description = $"Task '{t.Name}' ({t.Project?.Name}) is {t.Status}",
            Date = t.CreatedDate,
            Icon = t.Status == ProjectTaskStatus.Delayed ? "bi-exclamation-triangle" : "bi-check2-square",
            ColorClass = t.Status == ProjectTaskStatus.Delayed ? "text-danger" : "text-warning"
        }));
        vm.RecentActivities = activities.OrderByDescending(a => a.Date).Take(8).ToList();

        return vm;
    }

    public async Task<SalesAgentDashboardViewModel> BuildForSalesAgentAsync(string userId)
    {
        var mySales = await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Property)
            .Where(s => s.SalesAgentId == userId)
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync();

        var vm = new SalesAgentDashboardViewModel
        {
            MySalesCount = mySales.Count,
            MySalesTotal = mySales.Sum(s => s.FinalPrice),
            MyPendingBalanceCount = mySales.Count(s => s.RemainingBalance > 0),
            MyPendingBalanceTotal = mySales.Where(s => s.RemainingBalance > 0).Sum(s => s.RemainingBalance),
            MyCustomersCount = mySales.Select(s => s.CustomerId).Distinct().Count(),
            AvailablePropertiesCount = await _context.Properties.CountAsync(p => p.Status == PropertyStatus.Available),
            RecentSales = mySales.Take(6).ToList()
        };

        vm.PendingBookingRequests = await _context.BookingRequests
            .Include(b => b.Customer)
            .Include(b => b.Property)
            .Where(b => b.Status == BookingStatus.Pending)
            .OrderByDescending(b => b.RequestDate)
            .Take(6)
            .ToListAsync();
        vm.PendingBookingRequestsCount = await _context.BookingRequests.CountAsync(b => b.Status == BookingStatus.Pending);

        var now = DateTime.UtcNow;
        for (int i = 5; i >= 0; i--)
        {
            var month = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            var nextMonth = month.AddMonths(1);

            vm.ChartMonths.Add(month.ToString("MMM"));
            vm.ChartMySales.Add(mySales.Where(s => s.SaleDate >= month && s.SaleDate < nextMonth).Sum(s => s.FinalPrice));
        }

        return vm;
    }

    public async Task<AccountantDashboardViewModel> BuildForAccountantAsync()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var vm = new AccountantDashboardViewModel
        {
            TotalRevenue = await _context.Payments.SumAsync(p => (decimal?)p.Amount) ?? 0,
            RevenueThisMonth = await _context.Payments.Where(p => p.PaymentDate >= monthStart).SumAsync(p => (decimal?)p.Amount) ?? 0,
            PendingSalesBalance = await _context.Sales.Where(s => s.RemainingBalance > 0).SumAsync(s => (decimal?)s.RemainingBalance) ?? 0,
            PendingRentalBalance = await _context.Rentals.Where(r => r.PaymentStatus != PaymentStatus.Paid).SumAsync(r => (decimal?)r.MonthlyRent) ?? 0,
            OverdueSalesCount = await _context.Sales.CountAsync(s => s.PaymentStatus == PaymentStatus.Partial || s.PaymentStatus == PaymentStatus.Pending),
            OverdueRentalsCount = await _context.Rentals.CountAsync(r => r.EndDate < now && r.ContractStatus == ContractStatus.Active)
        };

        vm.RecentPayments = await _context.Payments
            .Include(p => p.Customer)
            .OrderByDescending(p => p.CreatedDate)
            .Take(8)
            .ToListAsync();

        vm.OutstandingSales = await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Property)
            .Where(s => s.RemainingBalance > 0)
            .OrderByDescending(s => s.RemainingBalance)
            .Take(6)
            .ToListAsync();

        for (int i = 7; i >= 0; i--)
        {
            var month = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            var nextMonth = month.AddMonths(1);

            var revenueInMonth = await _context.Payments
                .Where(p => p.PaymentDate >= month && p.PaymentDate < nextMonth)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            vm.ChartMonths.Add(month.ToString("MMM"));
            vm.ChartRevenue.Add(revenueInMonth);
        }

        return vm;
    }

    public async Task<CustomerDashboardViewModel> BuildForCustomerAsync(string userId)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.ApplicationUserId == userId);

        var vm = new CustomerDashboardViewModel { HasCustomerProfile = customer != null };
        if (customer == null) return vm;

        vm.MySales = await _context.Sales
            .Include(s => s.Property)
            .Where(s => s.CustomerId == customer.Id)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();

        vm.MyRentals = await _context.Rentals
            .Include(r => r.Property)
            .Where(r => r.CustomerId == customer.Id)
            .OrderByDescending(r => r.StartDate)
            .ToListAsync();

        vm.MyBookings = await _context.BookingRequests
            .Include(b => b.Property)
            .Where(b => b.CustomerId == customer.Id)
            .OrderByDescending(b => b.RequestDate)
            .Take(6)
            .ToListAsync();

        vm.RecentPayments = await _context.Payments
            .Where(p => p.CustomerId == customer.Id)
            .OrderByDescending(p => p.PaymentDate)
            .Take(6)
            .ToListAsync();

        vm.ActiveSalesCount = vm.MySales.Count(s => s.RemainingBalance > 0);
        vm.ActiveRentalsCount = vm.MyRentals.Count(r => r.ContractStatus == ContractStatus.Active);
        vm.PendingBookingsCount = vm.MyBookings.Count(b => b.Status == BookingStatus.Pending);

        vm.TotalPaid = await _context.Payments.Where(p => p.CustomerId == customer.Id).SumAsync(p => (decimal?)p.Amount) ?? 0;
        vm.TotalOutstanding = vm.MySales.Sum(s => s.RemainingBalance);

        return vm;
    }
}
