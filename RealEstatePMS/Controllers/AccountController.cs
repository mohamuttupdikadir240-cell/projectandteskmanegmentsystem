using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstatePMS.Data;
using RealEstatePMS.Models;
using RealEstatePMS.Models.Entities;
using RealEstatePMS.Models.Enums;
using RealEstatePMS.Services;
using RealEstatePMS.ViewModels;

namespace RealEstatePMS.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IFileStorageService _fileStorageService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IFileStorageService fileStorageService,
        ApplicationDbContext context,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _fileStorageService = fileStorageService;
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.EmailOrUsername)
                   ?? await _userManager.FindByNameAsync(model.EmailOrUsername);

        if (user == null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            if (Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);
            return RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "This account has been locked out. Please try again later.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var existing = await _userManager.FindByEmailAsync(model.Email);
        if (existing != null)
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            FullName = model.FullName,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            // Self-registrations default to the Customer role.
            await _userManager.AddToRoleAsync(user, Roles.Customer);

            _context.Customers.Add(new Customer
            {
                Name = user.FullName,
                Phone = user.PhoneNumber ?? string.Empty,
                Email = user.Email,
                ApplicationUserId = user.Id,
                RegistrationDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            // Do not reveal that the user does not exist.
            return RedirectToAction("ForgotPasswordConfirmation");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        // No email provider is configured for this demo; the reset link is
        // shown directly so the flow can be exercised end-to-end.
        var resetLink = Url.Action("ResetPassword", "Account", new { email = user.Email, token }, Request.Scheme);
        _logger.LogInformation("Password reset link for {Email}: {Link}", user.Email, resetLink);
        ViewBag.ResetLink = resetLink;

        return View("ForgotPasswordConfirmation");
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmation() => View();

    [HttpGet]
    public IActionResult ResetPassword(string? email = null, string? token = null)
    {
        if (email == null || token == null)
            return RedirectToAction("Login");

        return View(new ResetPasswordViewModel { Email = email, Token = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            return RedirectToAction("ResetPasswordConfirmation");

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (result.Succeeded)
            return RedirectToAction("ResetPasswordConfirmation");

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    [HttpGet]
    public IActionResult ResetPasswordConfirmation() => View();

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> ChangePassword()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login");
        return View(new ChangePasswordViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login");

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            TempData["StatusMessage"] = "Your password has been changed successfully.";
            return RedirectToAction("Profile");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login");

        var roles = await _userManager.GetRolesAsync(user);

        return View(new ProfileViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            ProfileImagePath = user.ProfileImagePath,
            Roles = roles,
            CreatedDate = user.CreatedDate
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login");

        if (!ModelState.IsValid)
        {
            model.Roles = await _userManager.GetRolesAsync(user);
            model.ProfileImagePath = user.ProfileImagePath;
            return View(model);
        }

        user.FullName = model.FullName;
        user.PhoneNumber = model.PhoneNumber;

        if (model.ProfileImageFile != null && _fileStorageService.IsAllowedFile(model.ProfileImageFile, new[] { ".jpg", ".jpeg", ".png", ".gif" }, 5 * 1024 * 1024))
        {
            var path = await _fileStorageService.SaveFileAsync(model.ProfileImageFile, "profiles");
            user.ProfileImagePath = path;
        }

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            TempData["StatusMessage"] = "Your profile has been updated.";
            return RedirectToAction("Profile");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        model.Roles = await _userManager.GetRolesAsync(user);
        return View(model);
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> NotificationSettings()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login");

        var roles = await _userManager.GetRolesAsync(user);
        var relevantTypes = NotificationRelevance.GetForRoles(roles);

        var preferences = await _context.NotificationPreferences
            .Where(p => p.UserId == user.Id)
            .ToDictionaryAsync(p => p.Type, p => p.IsEnabled);

        var model = new NotificationSettingsViewModel
        {
            Items = relevantTypes.Select(t => new NotificationSettingItem
            {
                Type = t.Type,
                Label = t.Label,
                Description = t.Description,
                IsEnabled = !preferences.TryGetValue(t.Type, out var enabled) || enabled
            }).ToList()
        };

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NotificationSettings(List<NotificationType> enabledTypes)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login");

        var roles = await _userManager.GetRolesAsync(user);
        var relevantTypes = NotificationRelevance.GetForRoles(roles).Select(t => t.Type).ToList();

        var existing = await _context.NotificationPreferences.Where(p => p.UserId == user.Id).ToListAsync();

        foreach (var type in relevantTypes)
        {
            var isEnabled = enabledTypes.Contains(type);
            var pref = existing.FirstOrDefault(p => p.Type == type);
            if (pref == null)
            {
                _context.NotificationPreferences.Add(new NotificationPreference
                {
                    UserId = user.Id,
                    Type = type,
                    IsEnabled = isEnabled
                });
            }
            else
            {
                pref.IsEnabled = isEnabled;
            }
        }

        await _context.SaveChangesAsync();
        TempData["StatusMessage"] = "Notification preferences updated.";
        return RedirectToAction(nameof(NotificationSettings));
    }
}
