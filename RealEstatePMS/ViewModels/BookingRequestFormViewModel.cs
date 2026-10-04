using System.ComponentModel.DataAnnotations;
using RealEstatePMS.Models.Enums;

namespace RealEstatePMS.ViewModels;

public class BookingRequestFormViewModel
{
    public int PropertyId { get; set; }
    public BookingType Type { get; set; }

    [Required, StringLength(150)]
    [Display(Name = "Full Name")]
    public string ContactName { get; set; } = string.Empty;

    [Required, Phone, StringLength(30)]
    [Display(Name = "Phone Number")]
    public string ContactPhone { get; set; } = string.Empty;

    [EmailAddress, StringLength(150)]
    [Display(Name = "Email Address")]
    public string? ContactEmail { get; set; }

    [Display(Name = "Preferred Payment Method")]
    public PaymentMethod PreferredPaymentMethod { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Please enter a valid amount.")]
    [Display(Name = "Offered Amount")]
    public decimal? ProposedAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Please enter a valid deposit amount.")]
    [Display(Name = "Deposit You Plan to Pay")]
    public decimal? ProposedDeposit { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public decimal ListedPrice { get; set; }
}
