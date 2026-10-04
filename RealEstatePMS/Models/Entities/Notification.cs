using System.ComponentModel.DataAnnotations;
using RealEstatePMS.Models.Enums;

namespace RealEstatePMS.Models.Entities;

public class Notification
{
    public int Id { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    [Required, StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Message { get; set; } = string.Empty;

    public NotificationType Type { get; set; }

    public bool IsRead { get; set; }

    public string? Link { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
