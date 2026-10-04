using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RealEstatePMS.Models.Enums;

namespace RealEstatePMS.Models.Entities;

public class Project
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    [Display(Name = "Project Name")]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(30)]
    [Display(Name = "Project Code")]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required, DataType(DataType.Date)]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; }

    [Required, DataType(DataType.Date)]
    [Display(Name = "Expected End Date")]
    public DateTime ExpectedEndDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Budget { get; set; }

    [Display(Name = "Project Manager")]
    public string? ProjectManagerId { get; set; }
    public ApplicationUser? ProjectManager { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;

    [Range(0, 100)]
    [Display(Name = "Progress %")]
    public int ProgressPercentage { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Property> Properties { get; set; } = new List<Property>();
    public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();

    [NotMapped]
    public int NumberOfProperties => Properties?.Count ?? 0;
}
