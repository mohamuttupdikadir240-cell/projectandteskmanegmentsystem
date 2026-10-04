using RealEstatePMS.Models.Enums;

namespace RealEstatePMS.ViewModels;

public class NotificationSettingsViewModel
{
    public List<NotificationSettingItem> Items { get; set; } = new();
}

public class NotificationSettingItem
{
    public NotificationType Type { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}
