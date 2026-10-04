using RealEstatePMS.Models.Entities;

namespace RealEstatePMS.Services;

public interface IRentalService
{
    Task<(bool Success, string? Error)> CreateRentalAsync(Rental rental);
    Task<(bool Success, string? Error)> UpdateRentalAsync(Rental rental);
    Task<(bool Success, string? Error)> DeleteRentalAsync(int id);
    Task RecalculatePaymentStatusAsync(int rentalId);
}
