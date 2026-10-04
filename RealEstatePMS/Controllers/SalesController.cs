using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RealEstatePMS.Data;
using RealEstatePMS.Filters;
using RealEstatePMS.Helpers;
using RealEstatePMS.Models;
using RealEstatePMS.Models.Entities;
using RealEstatePMS.Models.Enums;
using RealEstatePMS.Services;

namespace RealEstatePMS.Controllers;

[Authorize]
[ModulePermission(Modules.Sales)]
public class SalesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ISalesService _salesService;
    private readonly UserManager<ApplicationUser> _userManager;

    public SalesController(ApplicationDbContext context, ISalesService salesService, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _salesService = salesService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? search, PaymentStatus? status, int pageIndex = 1)
    {
        var query = _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Property)
            .Include(s => s.SalesAgent)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s => (s.Customer != null && s.Customer.Name.Contains(search))
                || (s.Property != null && s.Property.Code.Contains(search)));

        if (status.HasValue) query = query.Where(s => s.PaymentStatus == status.Value);

        query = query.OrderByDescending(s => s.CreatedDate);

        ViewBag.Search = search;
        ViewBag.Status = status;

        var result = await PaginatedList<Sale>.CreateAsync(query, pageIndex, 8);
        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        var sale = await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Property).ThenInclude(p => p!.Project)
            .Include(s => s.SalesAgent)
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sale == null) return NotFound();
        return View(sale);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateDropdownsAsync();
        return View(new Sale { SaleDate = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Sale sale)
    {
        ModelState.Remove(nameof(Sale.FinalPrice));
        ModelState.Remove(nameof(Sale.RemainingBalance));

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(sale.CustomerId, sale.PropertyId, sale.SalesAgentId);
            return View(sale);
        }

        var (success, error) = await _salesService.CreateSaleAsync(sale);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            await PopulateDropdownsAsync(sale.CustomerId, sale.PropertyId, sale.SalesAgentId);
            return View(sale);
        }

        TempData["StatusMessage"] = "Sale created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var sale = await _context.Sales.FindAsync(id);
        if (sale == null) return NotFound();
        await PopulateDropdownsAsync(sale.CustomerId, sale.PropertyId, sale.SalesAgentId, includeCurrentProperty: sale.PropertyId);
        return View(sale);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Sale sale)
    {
        if (id != sale.Id) return NotFound();
        ModelState.Remove(nameof(Sale.FinalPrice));
        ModelState.Remove(nameof(Sale.RemainingBalance));
        ModelState.Remove(nameof(Sale.PropertyId));

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(sale.CustomerId, sale.PropertyId, sale.SalesAgentId, includeCurrentProperty: sale.PropertyId);
            return View(sale);
        }

        var existing = await _context.Sales.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (existing == null) return NotFound();
        sale.PropertyId = existing.PropertyId;

        var (success, error) = await _salesService.UpdateSaleAsync(sale);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            await PopulateDropdownsAsync(sale.CustomerId, sale.PropertyId, sale.SalesAgentId, includeCurrentProperty: sale.PropertyId);
            return View(sale);
        }

        TempData["StatusMessage"] = "Sale updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var sale = await _context.Sales.Include(s => s.Customer).Include(s => s.Property).FirstOrDefaultAsync(s => s.Id == id);
        if (sale == null) return NotFound();
        return View(sale);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var (success, error) = await _salesService.DeleteSaleAsync(id);
        if (!success)
        {
            TempData["ErrorMessage"] = error;
        }
        else
        {
            TempData["StatusMessage"] = "Sale deleted and property released successfully.";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync(int? customerId = null, int? propertyId = null, string? agentId = null, int? includeCurrentProperty = null)
    {
        ViewBag.Customers = new SelectList(await _context.Customers.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", customerId);

        var propertyQuery = _context.Properties.Where(p => p.Status == PropertyStatus.Available);
        if (includeCurrentProperty.HasValue)
            propertyQuery = _context.Properties.Where(p => p.Status == PropertyStatus.Available || p.Id == includeCurrentProperty.Value);

        var properties = await propertyQuery.OrderBy(p => p.Code).ToListAsync();
        ViewBag.Properties = new SelectList(properties, "Id", "DisplayName", propertyId);

        var agents = await _userManager.GetUsersInRoleAsync(Roles.SalesAgent);
        ViewBag.Agents = new SelectList(agents, "Id", "FullName", agentId);
    }
}
