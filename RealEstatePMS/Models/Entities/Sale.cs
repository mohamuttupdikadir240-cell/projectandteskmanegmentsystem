using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RealEstatePMS.Models.Enums;

namespace RealEstatePMS.Models.Entities;

public class Sale
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Required]
    [Display(Name = "Property")]
    public int PropertyId { get; set; }
    public Property? Property { get; set; }

    [Required]
    [Display(Name = "Sales Agent")]
    public string SalesAgentId { get; set; } = string.Empty;
    public ApplicationUser? SalesAgent { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Sale Date")]
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Total Price")]
    public decimal TotalPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Discount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Final Price")]
    public decimal FinalPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Deposit { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Remaining Balance")]
    public decimal RemainingBalance { get; set; }

    [Display(Name = "Payment Status")]
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
