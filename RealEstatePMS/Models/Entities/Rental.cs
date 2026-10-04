using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RealEstatePMS.Models.Enums;

namespace RealEstatePMS.Models.Entities;

public class Rental
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Customer/Tenant")]
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Required]
    [Display(Name = "Property")]
    public int PropertyId { get; set; }
    public Property? Property { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Monthly Rent")]
    public decimal MonthlyRent { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "End Date")]
    public DateTime EndDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Deposit { get; set; }

    [Display(Name = "Payment Status")]
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    [Display(Name = "Contract Status")]
    public ContractStatus ContractStatus { get; set; } = ContractStatus.Active;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
