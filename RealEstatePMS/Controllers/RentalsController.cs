using Microsoft.AspNetCore.Authorization;
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
[ModulePermission(Modules.Rentals)]
public class RentalsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IRentalService _rentalService;
    private readonly IPaymentService _paymentService;

    public RentalsController(ApplicationDbContext context, IRentalService rentalService, IPaymentService paymentService)
    {
        _context = context;
        _rentalService = rentalService;
        _paymentService = paymentService;
    }

    public async Task<IActionResult> Index(string? search, ContractStatus? status, int pageIndex = 1)
    {
        var query = _context.Rentals.Include(r => r.Customer).Include(r => r.Property).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => (r.Customer != null && r.Customer.Name.Contains(search))
                || (r.Property != null && r.Property.Code.Contains(search)));

        if (status.HasValue) query = query.Where(r => r.ContractStatus == status.Value);

        query = query.OrderByDescending(r => r.CreatedDate);

        ViewBag.Search = search;
        ViewBag.Status = status;

        var result = await PaginatedList<Rental>.CreateAsync(query, pageIndex, 8);
        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        var rental = await _context.Rentals
            .Include(r => r.Customer)
            .Include(r => r.Property).ThenInclude(p => p!.Project)
            .Include(r => r.Payments)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rental == null) return NotFound();

        var (totalPaid, remaining, percentage) = await _paymentService.GetRentalSummaryAsync(id);
        ViewBag.TotalPaid = totalPaid;
        ViewBag.RemainingBalance = remaining;
        ViewBag.PaidPercentage = percentage;

        return View(rental);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateDropdownsAsync();
        return View(new Rental { StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Rental rental)
    {
        ModelState.Remove(nameof(Rental.PaymentStatus));
        ModelState.Remove(nameof(Rental.ContractStatus));

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(rental.CustomerId, rental.PropertyId);
            return View(rental);
        }

        var (success, error) = await _rentalService.CreateRentalAsync(rental);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            await PopulateDropdownsAsync(rental.CustomerId, rental.PropertyId);
            return View(rental);
        }

        TempData["StatusMessage"] = "Rental created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var rental = await _context.Rentals.FindAsync(id);
        if (rental == null) return NotFound();
        await PopulateDropdownsAsync(rental.CustomerId, rental.PropertyId, includeCurrentProperty: rental.PropertyId);
        return View(rental);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Rental rental)
    {
        if (id != rental.Id) return NotFound();
        ModelState.Remove(nameof(Rental.PropertyId));
        ModelState.Remove(nameof(Rental.PaymentStatus));

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(rental.CustomerId, rental.PropertyId, includeCurrentProperty: rental.PropertyId);
            return View(rental);
        }

        var existing = await _context.Rentals.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        if (existing == null) return NotFound();
        rental.PropertyId = existing.PropertyId;

        var (success, error) = await _rentalService.UpdateRentalAsync(rental);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            await PopulateDropdownsAsync(rental.CustomerId, rental.PropertyId, includeCurrentProperty: rental.PropertyId);
            return View(rental);
        }

        TempData["StatusMessage"] = "Rental updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var rental = await _context.Rentals.Include(r => r.Customer).Include(r => r.Property).FirstOrDefaultAsync(r => r.Id == id);
        if (rental == null) return NotFound();
        return View(rental);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var (success, error) = await _rentalService.DeleteRentalAsync(id);
        if (!success)
            TempData["ErrorMessage"] = error;
        else
            TempData["StatusMessage"] = "Rental deleted and property released successfully.";

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync(int? customerId = null, int? propertyId = null, int? includeCurrentProperty = null)
    {
        ViewBag.Customers = new SelectList(await _context.Customers.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", customerId);

        var propertyQuery = _context.Properties.Where(p => p.Status == PropertyStatus.Available);
        if (includeCurrentProperty.HasValue)
            propertyQuery = _context.Properties.Where(p => p.Status == PropertyStatus.Available || p.Id == includeCurrentProperty.Value);

        var properties = await propertyQuery.OrderBy(p => p.Code).ToListAsync();
        ViewBag.Properties = new SelectList(properties, "Id", "DisplayName", propertyId);
    }
}
