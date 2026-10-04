using RealEstatePMS.Models.Entities;

namespace RealEstatePMS.Services;

public interface IPaymentService
{
    Task<(bool Success, string? Error)> CreatePaymentAsync(Payment payment);
    Task<(bool Success, string? Error)> DeletePaymentAsync(int id);
    Task<(decimal TotalPaid, decimal Remaining, decimal Percentage)> GetSaleSummaryAsync(int saleId);
    Task<(decimal TotalPaid, decimal Remaining, decimal Percentage)> GetRentalSummaryAsync(int rentalId);
}
