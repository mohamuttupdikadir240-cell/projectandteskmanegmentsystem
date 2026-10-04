using RealEstatePMS.Models.Entities;

namespace RealEstatePMS.Services;

public interface ISalesService
{
    Task<(bool Success, string? Error)> CreateSaleAsync(Sale sale);
    Task<(bool Success, string? Error)> UpdateSaleAsync(Sale sale);
    Task<(bool Success, string? Error)> DeleteSaleAsync(int id);
    Task RecalculatePaymentStatusAsync(int saleId);
}
