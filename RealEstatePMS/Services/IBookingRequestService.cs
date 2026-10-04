using RealEstatePMS.Models.Entities;
using RealEstatePMS.Models.Enums;

namespace RealEstatePMS.Services;

public interface IBookingRequestService
{
    Task<(bool Success, string? Error)> CreateAsync(BookingRequest request);
    Task<(bool Success, string? Error)> ConfirmAsync(int bookingRequestId, string reviewerId, decimal agreedAmount, decimal deposit, PaymentStatus? paymentStatus, string? reviewNotes);
    Task<(bool Success, string? Error)> RejectAsync(int bookingRequestId, string reviewerId, string? reviewNotes);
}
