using Microsoft.EntityFrameworkCore;
using RealEstatePMS.Data;
using RealEstatePMS.Models;
using RealEstatePMS.Models.Entities;
using RealEstatePMS.Models.Enums;

namespace RealEstatePMS.Services;

public class SalesService : ISalesService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public SalesService(ApplicationDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<(bool Success, string? Error)> CreateSaleAsync(Sale sale)
    {
        var property = await _context.Properties.FindAsync(sale.PropertyId);
        if (property == null)
            return (false, "Property not found.");

        if (property.Status is PropertyStatus.Sold or PropertyStatus.Rented)
            return (false, "This property is not available for sale.");

        sale.FinalPrice = sale.TotalPrice - sale.Discount;
        sale.RemainingBalance = sale.FinalPrice - sale.Deposit;
        sale.PaymentStatus = CalculateStatus(sale.FinalPrice, sale.Deposit);

        property.Status = PropertyStatus.Sold;

        _context.Sales.Add(sale);
        await _context.SaveChangesAsync();

        if (sale.Deposit > 0)
        {
            _context.Payments.Add(new Payment
            {
                CustomerId = sale.CustomerId,
                SaleId = sale.Id,
                Amount = sale.Deposit,
                PaymentDate = sale.SaleDate,
                Method = PaymentMethod.Cash,
                Notes = "Initial deposit recorded automatically with the sale."
            });
            await _context.SaveChangesAsync();
        }

        var customer = await _context.Customers.FindAsync(sale.CustomerId);
        await _notificationService.NotifyRolesAsync(new[] { Roles.Admin, Roles.ProjectManager, Roles.Accountant },
            "New Sale Created",
            $"A new sale (#{sale.Id}) was created for {customer?.Name} - {property.Code}.",
            NotificationType.NewSale, $"/Sales/Details/{sale.Id}");

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateSaleAsync(Sale sale)
    {
        var existing = await _context.Sales.FindAsync(sale.Id);
        if (existing == null) return (false, "Sale not found.");

        existing.CustomerId = sale.CustomerId;
        existing.SalesAgentId = sale.SalesAgentId;
        existing.SaleDate = sale.SaleDate;
        existing.TotalPrice = sale.TotalPrice;
        existing.Discount = sale.Discount;
        existing.Deposit = sale.Deposit;
        existing.FinalPrice = existing.TotalPrice - existing.Discount;

        var paid = await _context.Payments.Where(p => p.SaleId == existing.Id).SumAsync(p => (decimal?)p.Amount) ?? 0;
        existing.RemainingBalance = existing.FinalPrice - Math.Max(paid, existing.Deposit);
        existing.PaymentStatus = CalculateStatus(existing.FinalPrice, Math.Max(paid, existing.Deposit));

        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteSaleAsync(int id)
    {
        var sale = await _context.Sales.Include(s => s.Property).Include(s => s.Documents).FirstOrDefaultAsync(s => s.Id == id);
        if (sale == null) return (false, "Sale not found.");

        if (sale.Property != null)
            sale.Property.Status = PropertyStatus.Available;

        var payments = await _context.Payments.Where(p => p.SaleId == id).ToListAsync();
        _context.Payments.RemoveRange(payments);
        _context.Documents.RemoveRange(sale.Documents);
        _context.Sales.Remove(sale);
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task RecalculatePaymentStatusAsync(int saleId)
    {
        var sale = await _context.Sales.FindAsync(saleId);
        if (sale == null) return;

        var totalPaid = await _context.Payments.Where(p => p.SaleId == saleId).SumAsync(p => (decimal?)p.Amount) ?? 0;
        sale.RemainingBalance = sale.FinalPrice - totalPaid;
        sale.PaymentStatus = CalculateStatus(sale.FinalPrice, totalPaid);
        await _context.SaveChangesAsync();
    }

    private static PaymentStatus CalculateStatus(decimal total, decimal paid)
    {
        if (paid <= 0) return PaymentStatus.Pending;
        if (paid >= total) return PaymentStatus.Paid;
        return PaymentStatus.Partial;
    }
}
