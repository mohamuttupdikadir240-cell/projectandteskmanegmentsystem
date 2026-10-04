using Microsoft.EntityFrameworkCore;
using RealEstatePMS.Data;
using RealEstatePMS.Models.Entities;
using RealEstatePMS.Models.Enums;

namespace RealEstatePMS.Services;

public class RentalService : IRentalService
{
    private readonly ApplicationDbContext _context;

    public RentalService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string? Error)> CreateRentalAsync(Rental rental)
    {
        var property = await _context.Properties.FindAsync(rental.PropertyId);
        if (property == null)
            return (false, "Property not found.");

        if (property.Status is PropertyStatus.Sold or PropertyStatus.Rented)
            return (false, "This property is not available for rent.");

        rental.PaymentStatus = rental.Deposit > 0 ? PaymentStatus.Partial : PaymentStatus.Pending;
        rental.ContractStatus = ContractStatus.Active;
        property.Status = PropertyStatus.Rented;

        _context.Rentals.Add(rental);
        await _context.SaveChangesAsync();

        if (rental.Deposit > 0)
        {
            _context.Payments.Add(new Payment
            {
                CustomerId = rental.CustomerId,
                RentalId = rental.Id,
                Amount = rental.Deposit,
                PaymentDate = rental.StartDate,
                Method = PaymentMethod.Cash,
                Notes = "Security deposit recorded automatically with the rental."
            });
            await _context.SaveChangesAsync();
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateRentalAsync(Rental rental)
    {
        var existing = await _context.Rentals.FindAsync(rental.Id);
        if (existing == null) return (false, "Rental not found.");

        existing.CustomerId = rental.CustomerId;
        existing.MonthlyRent = rental.MonthlyRent;
        existing.StartDate = rental.StartDate;
        existing.EndDate = rental.EndDate;
        existing.Deposit = rental.Deposit;
        existing.ContractStatus = rental.ContractStatus;

        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteRentalAsync(int id)
    {
        var rental = await _context.Rentals.Include(r => r.Property).Include(r => r.Documents).FirstOrDefaultAsync(r => r.Id == id);
        if (rental == null) return (false, "Rental not found.");

        if (rental.Property != null)
            rental.Property.Status = PropertyStatus.Available;

        var payments = await _context.Payments.Where(p => p.RentalId == id).ToListAsync();
        _context.Payments.RemoveRange(payments);
        _context.Documents.RemoveRange(rental.Documents);
        _context.Rentals.Remove(rental);
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task RecalculatePaymentStatusAsync(int rentalId)
    {
        var rental = await _context.Rentals.FindAsync(rentalId);
        if (rental == null) return;

        var totalPaid = await _context.Payments.Where(p => p.RentalId == rentalId).SumAsync(p => (decimal?)p.Amount) ?? 0;
        rental.PaymentStatus = totalPaid <= 0 ? PaymentStatus.Pending
            : totalPaid < rental.MonthlyRent ? PaymentStatus.Partial
            : PaymentStatus.Paid;

        if (rental.EndDate < DateTime.UtcNow.Date && rental.ContractStatus == ContractStatus.Active)
            rental.ContractStatus = ContractStatus.Expired;

        await _context.SaveChangesAsync();
    }
}
