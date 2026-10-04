using Microsoft.EntityFrameworkCore;
using RealEstatePMS.Data;
using RealEstatePMS.Models;
using RealEstatePMS.Models.Entities;
using RealEstatePMS.Models.Enums;

namespace RealEstatePMS.Services;

public class BookingRequestService : IBookingRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly ISalesService _salesService;
    private readonly IRentalService _rentalService;
    private readonly INotificationService _notificationService;

    public BookingRequestService(
        ApplicationDbContext context,
        ISalesService salesService,
        IRentalService rentalService,
        INotificationService notificationService)
    {
        _context = context;
        _salesService = salesService;
        _rentalService = rentalService;
        _notificationService = notificationService;
    }

    public async Task<(bool Success, string? Error)> CreateAsync(BookingRequest request)
    {
        var property = await _context.Properties.FindAsync(request.PropertyId);
        if (property == null)
            return (false, "Property not found.");

        if (property.Status != PropertyStatus.Available)
            return (false, "This property is no longer available.");

        var duplicate = await _context.BookingRequests.AnyAsync(b =>
            b.PropertyId == request.PropertyId &&
            b.CustomerId == request.CustomerId &&
            b.Status == BookingStatus.Pending);
        if (duplicate)
            return (false, "You already have a pending request for this property.");

        _context.BookingRequests.Add(request);
        await _context.SaveChangesAsync();

        request.BookingNumber = $"BK-{request.Id:D6}";
        await _context.SaveChangesAsync();

        var customer = await _context.Customers.FindAsync(request.CustomerId);
        await _notificationService.NotifyRolesAsync(
            new[] { Roles.Admin, Roles.ProjectManager, Roles.SalesAgent },
            "New Booking Request",
            $"{request.BookingNumber}: {customer?.Name} requested to {(request.Type == BookingType.Purchase ? "buy" : "rent")} {property.Code}.",
            NotificationType.NewSale,
            $"/BookingRequests/Index");

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ConfirmAsync(int bookingRequestId, string reviewerId, decimal agreedAmount, decimal deposit, PaymentStatus? paymentStatus, string? reviewNotes)
    {
        var request = await _context.BookingRequests
            .Include(b => b.Property)
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.Id == bookingRequestId);

        if (request == null) return (false, "Booking request not found.");
        if (request.Status != BookingStatus.Pending) return (false, "This request has already been reviewed.");
        if (request.Property == null) return (false, "Property not found.");
        if (agreedAmount <= 0)
        {
            return (false, request.Type == BookingType.Purchase
                ? "Please enter the agreed sale price."
                : "Please enter the monthly rent amount.");
        }

        if (request.Type == BookingType.Purchase)
        {
            var sale = new Sale
            {
                CustomerId = request.CustomerId,
                PropertyId = request.PropertyId,
                SalesAgentId = reviewerId,
                SaleDate = DateTime.UtcNow,
                TotalPrice = request.Property.Price,
                Discount = Math.Max(0, request.Property.Price - agreedAmount),
                Deposit = deposit
            };

            var (success, error) = await _salesService.CreateSaleAsync(sale);
            if (!success) return (false, error);

            if (paymentStatus.HasValue)
            {
                sale.PaymentStatus = paymentStatus.Value;
                await _context.SaveChangesAsync();
            }

            request.ResultingSaleId = sale.Id;
        }
        else
        {
            var rental = new Rental
            {
                CustomerId = request.CustomerId,
                PropertyId = request.PropertyId,
                MonthlyRent = agreedAmount,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddYears(1),
                Deposit = deposit
            };

            var (success, error) = await _rentalService.CreateRentalAsync(rental);
            if (!success) return (false, error);

            if (paymentStatus.HasValue)
            {
                rental.PaymentStatus = paymentStatus.Value;
                await _context.SaveChangesAsync();
            }

            request.ResultingRentalId = rental.Id;
        }

        request.Status = BookingStatus.Confirmed;
        request.ReviewedById = reviewerId;
        request.ReviewedDate = DateTime.UtcNow;
        request.ReviewNotes = reviewNotes;
        await _context.SaveChangesAsync();

        if (request.Customer?.ApplicationUserId != null)
        {
            await _notificationService.CreateAsync(
                request.Customer.ApplicationUserId,
                "Booking Confirmed",
                $"{request.BookingNumber}: Your request for {request.Property.Code} has been confirmed. Our team will contact you with payment details.",
                NotificationType.NewSale,
                request.Type == BookingType.Purchase
                    ? $"/Sales/Details/{request.ResultingSaleId}"
                    : $"/Rentals/Details/{request.ResultingRentalId}");
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RejectAsync(int bookingRequestId, string reviewerId, string? reviewNotes)
    {
        var request = await _context.BookingRequests
            .Include(b => b.Property)
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.Id == bookingRequestId);

        if (request == null) return (false, "Booking request not found.");
        if (request.Status != BookingStatus.Pending) return (false, "This request has already been reviewed.");

        request.Status = BookingStatus.Rejected;
        request.ReviewedById = reviewerId;
        request.ReviewedDate = DateTime.UtcNow;
        request.ReviewNotes = reviewNotes;
        await _context.SaveChangesAsync();

        if (request.Customer?.ApplicationUserId != null)
        {
            await _notificationService.CreateAsync(
                request.Customer.ApplicationUserId,
                "Booking Request Declined",
                $"{request.BookingNumber}: Your request for {request.Property?.Code} could not be confirmed." + (string.IsNullOrWhiteSpace(reviewNotes) ? "" : $" Reason: {reviewNotes}"),
                NotificationType.NewSale,
                "/BookingRequests/MyBookings");
        }

        return (true, null);
    }
}
