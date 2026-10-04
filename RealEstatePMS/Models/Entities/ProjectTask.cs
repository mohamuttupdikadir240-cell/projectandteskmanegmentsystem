using System.ComponentModel.DataAnnotations;
using RealEstatePMS.Models.Enums;

namespace RealEstatePMS.Models.Entities;

public class ProjectTask
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    [Display(Name = "Task Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Project")]
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    [Display(Name = "Assigned Employee")]
    public string? AssignedEmployeeId { get; set; }
    public ApplicationUser? AssignedEmployee { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Due Date")]
    public DateTime DueDate { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public ProjectTaskStatus Status { get; set; } = ProjectTaskStatus.Pending;

    [Range(0, 100)]
    public int Progress { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
