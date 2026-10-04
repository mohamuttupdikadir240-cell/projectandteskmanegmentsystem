namespace RealEstatePMS.Models.Entities;

public class PropertyImage
{
    public int Id { get; set; }

    public int PropertyId { get; set; }
    public Property? Property { get; set; }

    public string ImagePath { get; set; } = string.Empty;

    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
}
