using Microsoft.AspNetCore.Identity;

namespace RealEstatePMS.Models.Entities;

public class ApplicationUser : IdentityUser
{
    [PersonalData]
    public string FullName { get; set; } = string.Empty;

    public string? ProfileImagePath { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Project> ManagedProjects { get; set; } = new List<Project>();
    public ICollection<ProjectTask> AssignedTasks { get; set; } = new List<ProjectTask>();
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
