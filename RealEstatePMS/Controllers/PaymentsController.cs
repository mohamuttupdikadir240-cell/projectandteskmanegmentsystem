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
[ModulePermission(Modules.Payments)]
public class PaymentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IPaymentService _paymentService;

    public PaymentsController(ApplicationDbContext context, IPaymentService paymentService)
    {
        _context = context;
        _paymentService = paymentService;
    }

    public async Task<IActionResult> Index(string? search, PaymentMethod? method, int pageIndex = 1)
    {
        var query = _context.Payments.Include(p => p.Customer).Include(p => p.Sale).Include(p => p.Rental).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Customer != null && p.Customer.Name.Contains(search));

        if (method.HasValue) query = query.Where(p => p.Method == method.Value);

        query = query.OrderByDescending(p => p.PaymentDate);

        ViewBag.Search = search;
        ViewBag.Method = method;

        var result = await PaginatedList<Payment>.CreateAsync(query, pageIndex, 10);
        return View(result);
    }

    public async Task<IActionResult> Create(int? saleId = null, int? rentalId = null)
    {
        await PopulateDropdownsAsync();

        var payment = new Payment { PaymentDate = DateTime.Today };

        if (saleId.HasValue)
        {
            var sale = await _context.Sales.FindAsync(saleId.Value);
            if (sale != null)
            {
                payment.SaleId = sale.Id;
                payment.CustomerId = sale.CustomerId;
            }
        }
        else if (rentalId.HasValue)
        {
            var rental = await _context.Rentals.FindAsync(rentalId.Value);
            if (rental != null)
            {
                payment.RentalId = rental.Id;
                payment.CustomerId = rental.CustomerId;
            }
        }

        return View(payment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Payment payment)
    {
        if (payment.SaleId == null && payment.RentalId == null)
            ModelState.AddModelError(string.Empty, "Please select either a Sale or a Rental for this payment.");

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(payment.CustomerId);
            return View(payment);
        }

        var (success, error) = await _paymentService.CreatePaymentAsync(payment);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            await PopulateDropdownsAsync(payment.CustomerId);
            return View(payment);
        }

        TempData["StatusMessage"] = "Payment recorded successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var payment = await _context.Payments
            .Include(p => p.Customer)
            .Include(p => p.Sale).ThenInclude(s => s!.Property)
            .Include(p => p.Rental).ThenInclude(r => r!.Property)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null) return NotFound();
        return View(payment);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var payment = await _context.Payments.Include(p => p.Customer).FirstOrDefaultAsync(p => p.Id == id);
        if (payment == null) return NotFound();
        return View(payment);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var (success, error) = await _paymentService.DeletePaymentAsync(id);
        if (!success) TempData["ErrorMessage"] = error;
        else TempData["StatusMessage"] = "Payment deleted and balances recalculated.";

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync(int? customerId = null)
    {
        ViewBag.Customers = new SelectList(await _context.Customers.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", customerId);

        ViewBag.Sales = new SelectList(
            await _context.Sales.Include(s => s.Customer).Include(s => s.Property)
                .Where(s => s.RemainingBalance > 0)
                .Select(s => new { s.Id, Label = (s.Customer!.Name) + " - " + s.Property!.Code })
                .ToListAsync(), "Id", "Label");

        ViewBag.Rentals = new SelectList(
            await _context.Rentals.Include(r => r.Customer).Include(r => r.Property)
                .Where(r => r.ContractStatus == ContractStatus.Active)
                .Select(r => new { r.Id, Label = (r.Customer!.Name) + " - " + r.Property!.Code })
                .ToListAsync(), "Id", "Label");
    }
}
