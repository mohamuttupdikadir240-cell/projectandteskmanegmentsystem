using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RealEstatePMS.Models;
using RealEstatePMS.Models.Entities;
using RealEstatePMS.Services;

namespace RealEstatePMS.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(IDashboardService dashboardService, INotificationService notificationService, UserManager<ApplicationUser> userManager)
    {
        _dashboardService = dashboardService;
        _notificationService = notificationService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        await _notificationService.GenerateSystemNotificationsAsync();
        var userId = _userManager.GetUserId(User)!;

        if (User.IsInRole(Roles.Admin))
        {
            var vm = await _dashboardService.BuildAsync();
            return View("Index", vm);
        }

        if (User.IsInRole(Roles.ProjectManager))
        {
            var vm = await _dashboardService.BuildForProjectManagerAsync(userId);
            return View("ProjectManagerDashboard", vm);
        }

        if (User.IsInRole(Roles.Accountant))
        {
            var vm = await _dashboardService.BuildForAccountantAsync();
            return View("AccountantDashboard", vm);
        }

        if (User.IsInRole(Roles.SalesAgent))
        {
            var vm = await _dashboardService.BuildForSalesAgentAsync(userId);
            return View("SalesAgentDashboard", vm);
        }

        if (User.IsInRole(Roles.Customer))
        {
            var vm = await _dashboardService.BuildForCustomerAsync(userId);
            return View("CustomerDashboard", vm);
        }

        return View("Index", await _dashboardService.BuildAsync());
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
