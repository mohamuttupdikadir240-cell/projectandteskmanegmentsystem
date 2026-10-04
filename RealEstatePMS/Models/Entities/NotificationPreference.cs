using RealEstatePMS.Models.Enums;

namespace RealEstatePMS.Models.Entities;

public class NotificationPreference
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public NotificationType Type { get; set; }

    public bool IsEnabled { get; set; } = true;
}
