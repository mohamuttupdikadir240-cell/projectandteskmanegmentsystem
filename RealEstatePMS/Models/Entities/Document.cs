using System.ComponentModel.DataAnnotations;
using RealEstatePMS.Models.Enums;

namespace RealEstatePMS.Models.Entities;

public class Document
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Document Type")]
    public DocumentType Type { get; set; }

    [Required]
    public string FilePath { get; set; } = string.Empty;

    public string? FileName { get; set; }

    public long FileSize { get; set; }

    public int? ProjectId { get; set; }
    public Project? Project { get; set; }

    public int? PropertyId { get; set; }
    public Property? Property { get; set; }

    public int? SaleId { get; set; }
    public Sale? Sale { get; set; }

    public int? RentalId { get; set; }
    public Rental? Rental { get; set; }

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string? UploadedById { get; set; }
    public ApplicationUser? UploadedBy { get; set; }

    [Display(Name = "Uploaded Date")]
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
}
