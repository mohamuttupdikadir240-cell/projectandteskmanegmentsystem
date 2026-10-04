using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RealEstatePMS.Services;

namespace RealEstatePMS.Filters;

/// <summary>
/// Gates a controller/action behind a configurable module permission (see Settings > Roles &amp; Permissions).
/// Admins always pass. Combine with [Authorize] to also require authentication.
/// </summary>
public class ModulePermissionAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _module;

    public ModulePermissionAttribute(string module)
    {
        _module = module;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();

        if (context.HttpContext.User.Identity?.IsAuthenticated == true &&
            await permissionService.CanAccessAsync(context.HttpContext.User, _module))
        {
            await next();
            return;
        }

        context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
    }
}
