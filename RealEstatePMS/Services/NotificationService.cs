using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RealEstatePMS.Data;
using RealEstatePMS.Models.Entities;
using RealEstatePMS.Models.Enums;

namespace RealEstatePMS.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task CreateAsync(string? userId, string title, string message, NotificationType type, string? link = null)
    {
        if (userId != null && !await IsEnabledForUserAsync(userId, type))
            return;

        _context.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            Link = link
        });
        await _context.SaveChangesAsync();
    }

    public async Task NotifyRolesAsync(IEnumerable<string> roles, string title, string message, NotificationType type, string? link = null)
    {
        var userIds = new HashSet<string>();
        foreach (var role in roles)
        {
            var users = await _userManager.GetUsersInRoleAsync(role);
            foreach (var u in users) userIds.Add(u.Id);
        }

        var disabledUserIds = (await _context.NotificationPreferences
            .Where(p => userIds.Contains(p.UserId) && p.Type == type && !p.IsEnabled)
            .Select(p => p.UserId)
            .ToListAsync()).ToHashSet();

        foreach (var id in userIds)
        {
            if (disabledUserIds.Contains(id)) continue;

            _context.Notifications.Add(new Notification
            {
                UserId = id,
                Title = title,
                Message = message,
                Type = type,
                Link = link
            });
        }
        await _context.SaveChangesAsync();
    }

    private async Task<bool> IsEnabledForUserAsync(string userId, NotificationType type)
    {
        var preference = await _context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Type == type);
        return preference?.IsEnabled ?? true; // opt-out model: enabled unless explicitly disabled
    }

    public async Task<int> GetUnreadCountAsync(string userId) =>
        await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task MarkAsReadAsync(int notificationId, string userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        if (notification != null)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
        foreach (var n in notifications) n.IsRead = true;
        await _context.SaveChangesAsync();
    }

    public async Task GenerateSystemNotificationsAsync()
    {
        var managementRoles = new[] { Models.Roles.Admin, Models.Roles.ProjectManager, Models.Roles.Accountant };
        var today = DateTime.UtcNow.Date;

        // Pending / overdue sale payments
        var pendingSales = await _context.Sales
            .Include(s => s.Customer)
            .Where(s => s.PaymentStatus == PaymentStatus.Pending || s.PaymentStatus == PaymentStatus.Partial)
            .Where(s => s.RemainingBalance > 0)
            .ToListAsync();

        foreach (var sale in pendingSales)
        {
            bool exists = await _context.Notifications.AnyAsync(n =>
                n.Type == NotificationType.PendingPayment &&
                n.Link == $"/Sales/Details/{sale.Id}" &&
                n.CreatedDate.Date == today);
            if (!exists)
            {
                await NotifyRolesAsync(managementRoles, "Pending Payment",
                    $"Sale #{sale.Id} for {sale.Customer?.Name} has a remaining balance of {sale.RemainingBalance:C}.",
                    NotificationType.PendingPayment, $"/Sales/Details/{sale.Id}");
            }
        }

        // Rentals expiring within 30 days
        var expiringRentals = await _context.Rentals
            .Include(r => r.Customer)
            .Where(r => r.ContractStatus == ContractStatus.Active && r.EndDate <= today.AddDays(30) && r.EndDate >= today)
            .ToListAsync();

        foreach (var rental in expiringRentals)
        {
            bool exists = await _context.Notifications.AnyAsync(n =>
                n.Type == NotificationType.ExpiringContract &&
                n.Link == $"/Rentals/Details/{rental.Id}" &&
                n.CreatedDate.Date == today);
            if (!exists)
            {
                await NotifyRolesAsync(managementRoles, "Rental Contract Expiring",
                    $"Rental contract #{rental.Id} for {rental.Customer?.Name} expires on {rental.EndDate:d}.",
                    NotificationType.ExpiringContract, $"/Rentals/Details/{rental.Id}");
            }
        }

        // Delayed projects
        var delayedProjects = await _context.Projects
            .Where(p => p.Status != ProjectStatus.Completed && p.Status != ProjectStatus.Cancelled)
            .Where(p => p.ExpectedEndDate < today)
            .ToListAsync();

        foreach (var project in delayedProjects)
        {
            bool exists = await _context.Notifications.AnyAsync(n =>
                n.Type == NotificationType.DelayedProject &&
                n.Link == $"/Projects/Details/{project.Id}" &&
                n.CreatedDate.Date == today);
            if (!exists)
            {
                await NotifyRolesAsync(managementRoles, "Delayed Project",
                    $"Project '{project.Name}' has passed its expected end date ({project.ExpectedEndDate:d}).",
                    NotificationType.DelayedProject, $"/Projects/Details/{project.Id}");
            }
        }
    }
}
