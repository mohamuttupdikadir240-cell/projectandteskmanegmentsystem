using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RealEstatePMS.Models.Enums;

namespace RealEstatePMS.Models.Entities;

public class Property
{
    public int Id { get; set; }

    [Required, StringLength(30)]
    [Display(Name = "Property Code")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Project")]
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    [Display(Name = "Property Type")]
    public PropertyType Type { get; set; }

    [StringLength(50)]
    public string? Building { get; set; }

    [StringLength(20)]
    public string? Floor { get; set; }

    [StringLength(30)]
    [Display(Name = "Unit Number")]
    public string? UnitNumber { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    [Display(Name = "Area (sqm)")]
    public decimal Area { get; set; }

    public int Bedrooms { get; set; }

    public int Bathrooms { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    public PropertyStatus Status { get; set; } = PropertyStatus.Available;

    [StringLength(2000)]
    public string? Description { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<PropertyImage> Images { get; set; } = new List<PropertyImage>();
    public Sale? Sale { get; set; }
    public Rental? Rental { get; set; }
    public ICollection<Document> Documents { get; set; } = new List<Document>();

    [NotMapped]
    public string DisplayName => $"{Code} - {Type} - {UnitNumber}";
}
