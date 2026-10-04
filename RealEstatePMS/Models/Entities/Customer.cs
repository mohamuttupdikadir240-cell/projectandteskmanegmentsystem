using System.ComponentModel.DataAnnotations;

namespace RealEstatePMS.Models.Entities;

public class Customer
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    [Display(Name = "Customer Name")]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(30)]
    [Phone]
    public string Phone { get; set; } = string.Empty;

    [StringLength(150)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(250)]
    public string? Address { get; set; }

    [StringLength(80)]
    public string? Nationality { get; set; }

    [StringLength(50)]
    [Display(Name = "ID Number")]
    public string? IdNumber { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [Display(Name = "Registration Date")]
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

    public string? ApplicationUserId { get; set; }
    public ApplicationUser? ApplicationUser { get; set; }

    // Navigation
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<Rental> Rentals { get; set; } = new List<Rental>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
