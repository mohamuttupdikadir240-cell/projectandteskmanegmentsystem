using RealEstatePMS.Models.Enums;

namespace RealEstatePMS.Services;

public interface INotificationService
{
    Task CreateAsync(string? userId, string title, string message, NotificationType type, string? link = null);
    Task NotifyRolesAsync(IEnumerable<string> roles, string title, string message, NotificationType type, string? link = null);
    Task<int> GetUnreadCountAsync(string userId);
    Task MarkAsReadAsync(int notificationId, string userId);
    Task MarkAllAsReadAsync(string userId);
    Task GenerateSystemNotificationsAsync();
}
