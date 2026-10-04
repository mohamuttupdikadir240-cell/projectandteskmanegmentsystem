using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstatePMS.Data;
using RealEstatePMS.Models.Entities;

namespace RealEstatePMS.ViewComponents;

public class NotificationBellViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationBellViewComponent(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = _userManager.GetUserId(UserClaimsPrincipal);
        if (string.IsNullOrEmpty(userId))
            return View(new List<Notification>());

        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedDate)
            .Take(8)
            .ToListAsync();

        ViewBag.UnreadCount = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

        return View(notifications);
    }
}
