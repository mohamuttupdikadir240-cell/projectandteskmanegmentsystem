using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RealEstatePMS.Data;
using RealEstatePMS.Filters;
using RealEstatePMS.Models;
using RealEstatePMS.Models.Entities;
using RealEstatePMS.Services;
using RealEstatePMS.ViewModels;

namespace RealEstatePMS.Controllers;

[Authorize]
[ModulePermission(Modules.Reports)]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IExportService _exportService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsController(ApplicationDbContext context, IExportService exportService, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _exportService = exportService;
        _userManager = userManager;
    }

    public IActionResult Index() => View();

    public async Task<IActionResult> Sales(ReportFilterViewModel filter, string? export)
    {
        var query = _context.Sales.Include(s => s.Customer).Include(s => s.Property).Include(s => s.SalesAgent).AsQueryable();
        query = ApplyCommonFilters(query, filter, s => s.SaleDate, s => s.CustomerId, s => s.PropertyId, s => s.SalesAgentId);

        var sales = await query.OrderByDescending(s => s.SaleDate).ToListAsync();

        var headers = new[] { "Date", "Customer", "Property", "Sales Agent", "Total Price", "Discount", "Final Price", "Status" };
        var rows = sales.Select(s => new object?[]
        {
            s.SaleDate, s.Customer?.Name, s.Property?.Code, s.SalesAgent?.FullName, s.TotalPrice, s.Discount, s.FinalPrice, s.PaymentStatus.ToString()
        });

        if (export != null) return Export(export, "Sales Report", headers, rows);

        await PopulateFiltersAsync(filter);
        ViewBag.ReportTitle = "Sales Report";
        ViewBag.Total = sales.Sum(s => s.FinalPrice);
        return View("ReportTable", new ReportTableViewModel { Headers = headers, Rows = rows.ToList(), ReportKey = "Sales" });
    }

    public async Task<IActionResult> Revenue(ReportFilterViewModel filter, string? export)
    {
        var query = _context.Payments.Include(p => p.Customer).Include(p => p.Sale).Include(p => p.Rental).AsQueryable();

        if (filter.StartDate.HasValue) query = query.Where(p => p.PaymentDate >= filter.StartDate.Value);
        if (filter.EndDate.HasValue) query = query.Where(p => p.PaymentDate <= filter.EndDate.Value);
        if (filter.CustomerId.HasValue) query = query.Where(p => p.CustomerId == filter.CustomerId.Value);

        var payments = await query.OrderByDescending(p => p.PaymentDate).ToListAsync();

        var headers = new[] { "Date", "Customer", "Source", "Amount", "Method", "Reference" };
        var rows = payments.Select(p => new object?[]
        {
            p.PaymentDate, p.Customer?.Name,
            p.SaleId != null ? $"Sale #{p.SaleId}" : p.RentalId != null ? $"Rental #{p.RentalId}" : "-",
            p.Amount, p.Method.ToString(), p.ReferenceNumber
        });

        if (export != null) return Export(export, "Revenue Report", headers, rows);

        await PopulateFiltersAsync(filter);
        ViewBag.ReportTitle = "Revenue Report";
        ViewBag.Total = payments.Sum(p => p.Amount);
        return View("ReportTable", new ReportTableViewModel { Headers = headers, Rows = rows.ToList(), ReportKey = "Revenue" });
    }

    public async Task<IActionResult> Properties(ReportFilterViewModel filter, string? export)
    {
        var query = _context.Properties.Include(p => p.Project).AsQueryable();
        if (filter.ProjectId.HasValue) query = query.Where(p => p.ProjectId == filter.ProjectId.Value);
        if (filter.PropertyId.HasValue) query = query.Where(p => p.Id == filter.PropertyId.Value);

        var properties = await query.OrderBy(p => p.Code).ToListAsync();

        var headers = new[] { "Code", "Project", "Type", "Area", "Price", "Status", "Created" };
        var rows = properties.Select(p => new object?[]
        {
            p.Code, p.Project?.Name, p.Type.ToString(), p.Area, p.Price, p.Status.ToString(), p.CreatedDate
        });

        if (export != null) return Export(export, "Property Report", headers, rows);

        await PopulateFiltersAsync(filter);
        ViewBag.ReportTitle = "Property Report";
        ViewBag.Total = properties.Sum(p => p.Price);
        return View("ReportTable", new ReportTableViewModel { Headers = headers, Rows = rows.ToList(), ReportKey = "Properties" });
    }

    public async Task<IActionResult> Customers(ReportFilterViewModel filter, string? export)
    {
        var query = _context.Customers.AsQueryable();
        if (filter.StartDate.HasValue) query = query.Where(c => c.RegistrationDate >= filter.StartDate.Value);
        if (filter.EndDate.HasValue) query = query.Where(c => c.RegistrationDate <= filter.EndDate.Value);

        var customers = await query.OrderByDescending(c => c.RegistrationDate).ToListAsync();

        var headers = new[] { "Name", "Phone", "Email", "Nationality", "Registration Date" };
        var rows = customers.Select(c => new object?[] { c.Name, c.Phone, c.Email, c.Nationality, c.RegistrationDate });

        if (export != null) return Export(export, "Customer Report", headers, rows);

        await PopulateFiltersAsync(filter);
        ViewBag.ReportTitle = "Customer Report";
        ViewBag.Total = customers.Count;
        return View("ReportTable", new ReportTableViewModel { Headers = headers, Rows = rows.ToList(), ReportKey = "Customers" });
    }

    public async Task<IActionResult> Payments(ReportFilterViewModel filter, string? export)
    {
        var query = _context.Payments.Include(p => p.Customer).AsQueryable();
        if (filter.StartDate.HasValue) query = query.Where(p => p.PaymentDate >= filter.StartDate.Value);
        if (filter.EndDate.HasValue) query = query.Where(p => p.PaymentDate <= filter.EndDate.Value);
        if (filter.CustomerId.HasValue) query = query.Where(p => p.CustomerId == filter.CustomerId.Value);

        var payments = await query.OrderByDescending(p => p.PaymentDate).ToListAsync();

        var headers = new[] { "Date", "Customer", "Amount", "Method", "Reference", "Notes" };
        var rows = payments.Select(p => new object?[] { p.PaymentDate, p.Customer?.Name, p.Amount, p.Method.ToString(), p.ReferenceNumber, p.Notes });

        if (export != null) return Export(export, "Payment Report", headers, rows);

        await PopulateFiltersAsync(filter);
        ViewBag.ReportTitle = "Payment Report";
        ViewBag.Total = payments.Sum(p => p.Amount);
        return View("ReportTable", new ReportTableViewModel { Headers = headers, Rows = rows.ToList(), ReportKey = "Payments" });
    }

    public async Task<IActionResult> ProjectProgress(ReportFilterViewModel filter, string? export)
    {
        var query = _context.Projects.Include(p => p.ProjectManager).Include(p => p.Properties).AsQueryable();
        if (filter.ProjectId.HasValue) query = query.Where(p => p.Id == filter.ProjectId.Value);

        var projects = await query.OrderBy(p => p.Name).ToListAsync();

        var headers = new[] { "Project", "Manager", "Status", "Progress %", "Properties", "Budget", "Expected End" };
        var rows = projects.Select(p => new object?[]
        {
            p.Name, p.ProjectManager?.FullName, p.Status.ToString(), p.ProgressPercentage, p.Properties.Count, p.Budget, p.ExpectedEndDate
        });

        if (export != null) return Export(export, "Project Progress Report", headers, rows);

        await PopulateFiltersAsync(filter);
        ViewBag.ReportTitle = "Project Progress Report";
        ViewBag.Total = projects.Count;
        return View("ReportTable", new ReportTableViewModel { Headers = headers, Rows = rows.ToList(), ReportKey = "ProjectProgress" });
    }

    public async Task<IActionResult> Rentals(ReportFilterViewModel filter, string? export)
    {
        var query = _context.Rentals.Include(r => r.Customer).Include(r => r.Property).AsQueryable();
        query = ApplyCommonFilters(query, filter, r => r.StartDate, r => r.CustomerId, r => r.PropertyId, null);

        var rentals = await query.OrderByDescending(r => r.StartDate).ToListAsync();

        var headers = new[] { "Tenant", "Property", "Monthly Rent", "Start Date", "End Date", "Payment Status", "Contract Status" };
        var rows = rentals.Select(r => new object?[]
        {
            r.Customer?.Name, r.Property?.Code, r.MonthlyRent, r.StartDate, r.EndDate, r.PaymentStatus.ToString(), r.ContractStatus.ToString()
        });

        if (export != null) return Export(export, "Rental Report", headers, rows);

        await PopulateFiltersAsync(filter);
        ViewBag.ReportTitle = "Rental Report";
        ViewBag.Total = rentals.Sum(r => r.MonthlyRent);
        return View("ReportTable", new ReportTableViewModel { Headers = headers, Rows = rows.ToList(), ReportKey = "Rentals" });
    }

    private IActionResult Export(string format, string title, string[] headers, IEnumerable<object?[]> rows)
    {
        if (format.Equals("excel", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = _exportService.ExportToExcel(title, headers, rows);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{title.Replace(" ", "_")}.xlsx");
        }

        var pdfBytes = _exportService.ExportToPdf(title, headers, rows, $"Generated by {User.Identity?.Name}");
        return File(pdfBytes, "application/pdf", $"{title.Replace(" ", "_")}.pdf");
    }

    private static IQueryable<T> ApplyCommonFilters<T>(IQueryable<T> query, ReportFilterViewModel filter,
        System.Linq.Expressions.Expression<Func<T, DateTime>> dateSelector,
        System.Linq.Expressions.Expression<Func<T, int>> customerSelector,
        System.Linq.Expressions.Expression<Func<T, int>> propertySelector,
        System.Linq.Expressions.Expression<Func<T, string>>? agentSelector)
    {
        if (filter.StartDate.HasValue)
        {
            var param = dateSelector.Parameters[0];
            var body = System.Linq.Expressions.Expression.GreaterThanOrEqual(dateSelector.Body, System.Linq.Expressions.Expression.Constant(filter.StartDate.Value));
            query = query.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(body, param));
        }
        if (filter.EndDate.HasValue)
        {
            var param = dateSelector.Parameters[0];
            var body = System.Linq.Expressions.Expression.LessThanOrEqual(dateSelector.Body, System.Linq.Expressions.Expression.Constant(filter.EndDate.Value));
            query = query.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(body, param));
        }
        if (filter.CustomerId.HasValue)
        {
            var param = customerSelector.Parameters[0];
            var body = System.Linq.Expressions.Expression.Equal(customerSelector.Body, System.Linq.Expressions.Expression.Constant(filter.CustomerId.Value));
            query = query.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(body, param));
        }
        if (filter.PropertyId.HasValue)
        {
            var param = propertySelector.Parameters[0];
            var body = System.Linq.Expressions.Expression.Equal(propertySelector.Body, System.Linq.Expressions.Expression.Constant(filter.PropertyId.Value));
            query = query.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(body, param));
        }
        if (filter.SalesAgentId != null && agentSelector != null)
        {
            var param = agentSelector.Parameters[0];
            var body = System.Linq.Expressions.Expression.Equal(agentSelector.Body, System.Linq.Expressions.Expression.Constant(filter.SalesAgentId));
            query = query.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(body, param));
        }

        return query;
    }

    private async Task PopulateFiltersAsync(ReportFilterViewModel filter)
    {
        ViewBag.Filter = filter;
        ViewBag.Projects = new SelectList(await _context.Projects.OrderBy(p => p.Name).ToListAsync(), "Id", "Name", filter.ProjectId);
        ViewBag.PropertiesList = new SelectList(await _context.Properties.OrderBy(p => p.Code).ToListAsync(), "Id", "Code", filter.PropertyId);
        ViewBag.Customers = new SelectList(await _context.Customers.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", filter.CustomerId);
        ViewBag.Agents = new SelectList(await _userManager.GetUsersInRoleAsync(Roles.SalesAgent), "Id", "FullName", filter.SalesAgentId);
    }
}
