using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstatePMS.Data;
using RealEstatePMS.Filters;
using RealEstatePMS.Helpers;
using RealEstatePMS.Models;
using RealEstatePMS.Models.Entities;
using RealEstatePMS.Models.Enums;
using RealEstatePMS.Services;
using RealEstatePMS.ViewModels;

namespace RealEstatePMS.Controllers;

[Authorize]
public class BookingRequestsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IBookingRequestService _bookingRequestService;

    public BookingRequestsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IBookingRequestService bookingRequestService)
    {
        _context = context;
        _userManager = userManager;
        _bookingRequestService = bookingRequestService;
    }

    // ---------- Customer-facing: browse available properties ----------

    [Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> Catalog(string? search, PropertyType? type, int pageIndex = 1)
    {
        var query = _context.Properties
            .Include(p => p.Project)
            .Include(p => p.Images)
            .Where(p => p.Status == PropertyStatus.Available)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Code.Contains(search) || (p.UnitNumber != null && p.UnitNumber.Contains(search)));

        if (type.HasValue) query = query.Where(p => p.Type == type.Value);

        query = query.OrderByDescending(p => p.CreatedDate);

        ViewBag.Search = search;
        ViewBag.Type = type;

        var result = await PaginatedList<Property>.CreateAsync(query, pageIndex, 9);
        return View(result);
    }

    [Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> Create(int propertyId, BookingType type)
    {
        var property = await _context.Properties.Include(p => p.Project).FirstOrDefaultAsync(p => p.Id == propertyId);
        if (property == null) return NotFound();

        if (property.Status != PropertyStatus.Available)
        {
            TempData["ErrorMessage"] = "This property is no longer available.";
            return RedirectToAction(nameof(Catalog));
        }

        ViewBag.Property = property;

        var userId = _userManager.GetUserId(User)!;
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.ApplicationUserId == userId);

        return View(new BookingRequestFormViewModel
        {
            PropertyId = propertyId,
            Type = type,
            ContactName = customer?.Name ?? string.Empty,
            ContactPhone = customer?.Phone ?? string.Empty,
            ContactEmail = customer?.Email,
            ProposedAmount = property.Price,
            ListedPrice = property.Price
        });
    }

    [Authorize(Roles = Roles.Customer)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingRequestFormViewModel model)
    {
        var userId = _userManager.GetUserId(User)!;
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.ApplicationUserId == userId);
        if (customer == null)
        {
            TempData["ErrorMessage"] = "We could not find a customer profile linked to your account.";
            return RedirectToAction(nameof(Catalog));
        }

        if (!ModelState.IsValid)
        {
            var property = await _context.Properties.Include(p => p.Project).FirstOrDefaultAsync(p => p.Id == model.PropertyId);
            if (property == null)
            {
                TempData["ErrorMessage"] = "This property could not be found.";
                return RedirectToAction(nameof(Catalog));
            }

            ViewBag.Property = property;
            return View(model);
        }

        // Keep the customer's profile in sync with the contact details given for this booking.
        customer.Name = model.ContactName;
        customer.Phone = model.ContactPhone;
        customer.Email = model.ContactEmail;
        await _context.SaveChangesAsync();

        var request = new BookingRequest
        {
            PropertyId = model.PropertyId,
            Type = model.Type,
            CustomerId = customer.Id,
            PreferredPaymentMethod = model.PreferredPaymentMethod,
            ProposedAmount = model.ProposedAmount,
            ProposedDeposit = model.ProposedDeposit,
            Notes = model.Notes
        };

        var (success, error) = await _bookingRequestService.CreateAsync(request);
        if (!success)
        {
            TempData["ErrorMessage"] = error;
            return RedirectToAction(nameof(Catalog));
        }

        TempData["StatusMessage"] = "Your request has been submitted. Our team will review and confirm it shortly.";
        return RedirectToAction(nameof(MyBookings));
    }

    [Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> MyBookings()
    {
        var userId = _userManager.GetUserId(User)!;
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.ApplicationUserId == userId);
        if (customer == null) return View(new List<BookingRequest>());

        var bookings = await _context.BookingRequests
            .Include(b => b.Property).ThenInclude(p => p!.Project)
            .Where(b => b.CustomerId == customer.Id)
            .OrderByDescending(b => b.RequestDate)
            .ToListAsync();

        return View(bookings);
    }

    // ---------- Staff-facing: review queue ----------

    [ModulePermission(Modules.BookingRequests)]
    public async Task<IActionResult> Index(BookingStatus? status)
    {
        var query = _context.BookingRequests
            .Include(b => b.Customer)
            .Include(b => b.Property)
            .AsQueryable();

        if (status.HasValue) query = query.Where(b => b.Status == status.Value);
        else query = query.Where(b => b.Status == BookingStatus.Pending);

        query = query.OrderByDescending(b => b.RequestDate);

        ViewBag.Status = status;

        var bookings = await query.ToListAsync();
        return View(bookings);
    }

    [ModulePermission(Modules.BookingRequests)]
    public async Task<IActionResult> Confirm(int id)
    {
        var request = await _context.BookingRequests
            .Include(b => b.Customer)
            .Include(b => b.Property).ThenInclude(p => p!.Project)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (request == null) return NotFound();
        if (request.Status != BookingStatus.Pending)
        {
            TempData["ErrorMessage"] = "This request has already been reviewed.";
            return RedirectToAction(nameof(Index));
        }

        return View(request);
    }

    [ModulePermission(Modules.BookingRequests)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id, decimal agreedAmount, decimal deposit, PaymentStatus? paymentStatus, string? reviewNotes)
    {
        var reviewerId = _userManager.GetUserId(User)!;
        var (success, error) = await _bookingRequestService.ConfirmAsync(id, reviewerId, agreedAmount, deposit, paymentStatus, reviewNotes);

        if (!success)
        {
            TempData["ErrorMessage"] = error;
            return RedirectToAction(nameof(Confirm), new { id });
        }

        TempData["StatusMessage"] = "Booking confirmed. The sale/rental record has been created and the customer notified.";
        return RedirectToAction(nameof(Index));
    }

    [ModulePermission(Modules.BookingRequests)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? reviewNotes)
    {
        var reviewerId = _userManager.GetUserId(User)!;
        var (success, error) = await _bookingRequestService.RejectAsync(id, reviewerId, reviewNotes);

        TempData[success ? "StatusMessage" : "ErrorMessage"] = success
            ? "Booking request declined and the customer notified."
            : error;

        return RedirectToAction(nameof(Index));
    }
}
