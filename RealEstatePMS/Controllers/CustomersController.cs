using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstatePMS.Data;
using RealEstatePMS.Filters;
using RealEstatePMS.Helpers;
using RealEstatePMS.Models;
using RealEstatePMS.Models.Entities;

namespace RealEstatePMS.Controllers;

[Authorize]
[ModulePermission(Modules.Customers)]
public class CustomersController : Controller
{
    private readonly ApplicationDbContext _context;

    public CustomersController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? nationality, int pageIndex = 1)
    {
        var query = _context.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search) || c.Phone.Contains(search) || (c.Email != null && c.Email.Contains(search)));

        if (!string.IsNullOrWhiteSpace(nationality))
            query = query.Where(c => c.Nationality == nationality);

        query = query.OrderByDescending(c => c.RegistrationDate);

        ViewBag.Search = search;
        ViewBag.Nationality = nationality;
        ViewBag.Nationalities = await _context.Customers
            .Where(c => c.Nationality != null)
            .Select(c => c.Nationality!)
            .Distinct()
            .ToListAsync();

        var result = await PaginatedList<Customer>.CreateAsync(query, pageIndex, 8);
        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        var customer = await _context.Customers
            .Include(c => c.Sales).ThenInclude(s => s.Property)
            .Include(c => c.Rentals).ThenInclude(r => r.Property)
            .Include(c => c.Payments)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer == null) return NotFound();
        return View(customer);
    }

    public IActionResult Create() => View(new Customer { RegistrationDate = DateTime.Today });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer customer)
    {
        if (!ModelState.IsValid) return View(customer);

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Customer created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();
        return View(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Customer customer)
    {
        if (id != customer.Id) return NotFound();
        if (!ModelState.IsValid) return View(customer);

        try
        {
            _context.Update(customer);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Customers.AnyAsync(c => c.Id == id)) return NotFound();
            throw;
        }

        TempData["StatusMessage"] = "Customer updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();
        return View(customer);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var hasSalesOrRentals = await _context.Sales.AnyAsync(s => s.CustomerId == id)
            || await _context.Rentals.AnyAsync(r => r.CustomerId == id);

        if (hasSalesOrRentals)
        {
            TempData["ErrorMessage"] = "Cannot delete a customer with existing sales or rentals.";
            return RedirectToAction(nameof(Index));
        }

        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();

        var documents = await _context.Documents.Where(d => d.CustomerId == id).ToListAsync();
        _context.Documents.RemoveRange(documents);
        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();
        TempData["StatusMessage"] = "Customer deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}
