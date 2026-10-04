using RealEstatePMS.Models.Enums;

namespace RealEstatePMS.Models.Entities;

public class ProjectStatusHistory
{
    public int Id { get; set; }

    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public ProjectStatus OldStatus { get; set; }
    public ProjectStatus NewStatus { get; set; }

    public int OldProgress { get; set; }
    public int NewProgress { get; set; }

    public string? Reason { get; set; }

    public string? ChangedById { get; set; }
    public ApplicationUser? ChangedBy { get; set; }

    public DateTime ChangedDate { get; set; } = DateTime.UtcNow;
}
