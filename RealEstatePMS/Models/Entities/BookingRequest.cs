using System.ComponentModel.DataAnnotations;
using RealEstatePMS.Models.Enums;

namespace RealEstatePMS.Models.Entities;

public class BookingRequest
{
    public int Id { get; set; }

    [StringLength(20)]
    public string BookingNumber { get; set; } = string.Empty;

    [Required]
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Required]
    public int PropertyId { get; set; }
    public Property? Property { get; set; }

    [Display(Name = "Request Type")]
    public BookingType Type { get; set; }

    [Display(Name = "Preferred Payment Method")]
    public PaymentMethod PreferredPaymentMethod { get; set; }

    [Display(Name = "Offered Amount")]
    public decimal? ProposedAmount { get; set; }

    [Display(Name = "Deposit You Plan to Pay")]
    public decimal? ProposedDeposit { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public DateTime RequestDate { get; set; } = DateTime.UtcNow;

    public string? ReviewedById { get; set; }
    public ApplicationUser? ReviewedBy { get; set; }

    public DateTime? ReviewedDate { get; set; }

    [StringLength(500)]
    public string? ReviewNotes { get; set; }

    public int? ResultingSaleId { get; set; }
    public Sale? ResultingSale { get; set; }

    public int? ResultingRentalId { get; set; }
    public Rental? ResultingRental { get; set; }
}
