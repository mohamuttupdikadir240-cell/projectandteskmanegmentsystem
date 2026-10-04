using RealEstatePMS.Models.Entities;

namespace RealEstatePMS.ViewModels;

public class PublicPropertiesViewModel
{
    public List<Property> AvailableProperties { get; set; } = new();
    public List<Property> RentedProperties { get; set; } = new();
    public string? Search { get; set; }
}
