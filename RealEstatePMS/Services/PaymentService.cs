using Microsoft.EntityFrameworkCore;
using RealEstatePMS.Data;
using RealEstatePMS.Models;
using RealEstatePMS.Models.Entities;
using RealEstatePMS.Models.Enums;

namespace RealEstatePMS.Services;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly ISalesService _salesService;
    private readonly IRentalService _rentalService;
    private readonly INotificationService _notificationService;

    public PaymentService(ApplicationDbContext context, ISalesService salesService, IRentalService rentalService, INotificationService notificationService)
    {
        _context = context;
        _salesService = salesService;
        _rentalService = rentalService;
        _notificationService = notificationService;
    }

    public async Task<(bool Success, string? Error)> CreatePaymentAsync(Payment payment)
    {
        if (payment.SaleId == null && payment.RentalId == null)
            return (false, "A payment must be linked to a Sale or a Rental.");

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        if (payment.SaleId.HasValue)
            await _salesService.RecalculatePaymentStatusAsync(payment.SaleId.Value);

        if (payment.RentalId.HasValue)
            await _rentalService.RecalculatePaymentStatusAsync(payment.RentalId.Value);

        await SendPaymentNotificationsAsync(payment);

        return (true, null);
    }

    private async Task SendPaymentNotificationsAsync(Payment payment)
    {
        var customer = await _context.Customers.FindAsync(payment.CustomerId);
        var source = payment.SaleId.HasValue ? $"Sale #{payment.SaleId}" : $"Rental #{payment.RentalId}";
        var link = payment.SaleId.HasValue ? $"/Sales/Details/{payment.SaleId}" : $"/Rentals/Details/{payment.RentalId}";

        await _notificationService.NotifyRolesAsync(
            new[] { Roles.Admin, Roles.ProjectManager, Roles.Accountant, Roles.SalesAgent },
            "Payment Received",
            $"{payment.Amount:C0} received from {customer?.Name} for {source}.",
            NotificationType.PaymentReceived,
            link);

        if (customer?.ApplicationUserId != null)
        {
            await _notificationService.CreateAsync(
                customer.ApplicationUserId,
                "Payment Confirmed",
                $"Your payment of {payment.Amount:C0} for {source} has been received. Thank you!",
                NotificationType.PaymentReceived,
                link);
        }
    }

    public async Task<(bool Success, string? Error)> DeletePaymentAsync(int id)
    {
        var payment = await _context.Payments.FindAsync(id);
        if (payment == null) return (false, "Payment not found.");

        var saleId = payment.SaleId;
        var rentalId = payment.RentalId;

        _context.Payments.Remove(payment);
        await _context.SaveChangesAsync();

        if (saleId.HasValue) await _salesService.RecalculatePaymentStatusAsync(saleId.Value);
        if (rentalId.HasValue) await _rentalService.RecalculatePaymentStatusAsync(rentalId.Value);

        return (true, null);
    }

    public async Task<(decimal TotalPaid, decimal Remaining, decimal Percentage)> GetSaleSummaryAsync(int saleId)
    {
        var sale = await _context.Sales.FindAsync(saleId);
        if (sale == null) return (0, 0, 0);

        var totalPaid = await _context.Payments.Where(p => p.SaleId == saleId).SumAsync(p => (decimal?)p.Amount) ?? 0;
        var remaining = sale.FinalPrice - totalPaid;
        var percentage = sale.FinalPrice > 0 ? Math.Round(totalPaid / sale.FinalPrice * 100, 1) : 0;
        return (totalPaid, remaining, percentage);
    }

    public async Task<(decimal TotalPaid, decimal Remaining, decimal Percentage)> GetRentalSummaryAsync(int rentalId)
    {
        var rental = await _context.Rentals.FindAsync(rentalId);
        if (rental == null) return (0, 0, 0);

        var totalPaid = await _context.Payments.Where(p => p.RentalId == rentalId).SumAsync(p => (decimal?)p.Amount) ?? 0;
        var remaining = rental.MonthlyRent - totalPaid;
        var percentage = rental.MonthlyRent > 0 ? Math.Round(totalPaid / rental.MonthlyRent * 100, 1) : 0;
        return (totalPaid, remaining, percentage);
    }
}
