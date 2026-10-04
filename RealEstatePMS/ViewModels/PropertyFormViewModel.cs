using System.ComponentModel.DataAnnotations;
using RealEstatePMS.Models.Entities;
using RealEstatePMS.Models.Enums;

namespace RealEstatePMS.ViewModels;

public class PropertyFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(30)]
    [Display(Name = "Property Code")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Project")]
    public int ProjectId { get; set; }

    [Display(Name = "Property Type")]
    public PropertyType Type { get; set; }

    [StringLength(50)]
    public string? Building { get; set; }

    [StringLength(20)]
    public string? Floor { get; set; }

    [StringLength(30)]
    [Display(Name = "Unit Number")]
    public string? UnitNumber { get; set; }

    [Display(Name = "Area (sqm)")]
    public decimal Area { get; set; }

    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
    public decimal Price { get; set; }

    public PropertyStatus Status { get; set; } = PropertyStatus.Available;

    [StringLength(2000)]
    public string? Description { get; set; }

    public List<IFormFile>? NewImages { get; set; }

    public List<PropertyImage> ExistingImages { get; set; } = new();
}
