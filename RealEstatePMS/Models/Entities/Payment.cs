using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RealEstatePMS.Models.Enums;

namespace RealEstatePMS.Models.Entities;

public class Payment
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Display(Name = "Sale")]
    public int? SaleId { get; set; }
    public Sale? Sale { get; set; }

    [Display(Name = "Rental")]
    public int? RentalId { get; set; }
    public Rental? Rental { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Payment Date")]
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    [Display(Name = "Payment Method")]
    public PaymentMethod Method { get; set; }

    [StringLength(60)]
    [Display(Name = "Reference Number")]
    public string? ReferenceNumber { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
