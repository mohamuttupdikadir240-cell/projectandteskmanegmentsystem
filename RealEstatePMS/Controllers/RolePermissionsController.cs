using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstatePMS.Models;
using RealEstatePMS.Services;

namespace RealEstatePMS.Controllers;

[Authorize(Roles = Roles.Admin)]
public class RolePermissionsController : Controller
{
    private readonly IPermissionService _permissionService;

    public RolePermissionsController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public async Task<IActionResult> Index()
    {
        var matrix = await _permissionService.GetMatrixAsync();
        ViewBag.Matrix = matrix;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(string role, string module, bool isAllowed)
    {
        if (!Modules.ConfigurableRoles.Contains(role) || !Modules.All.Any(m => m.Key == module))
            return BadRequest();

        await _permissionService.SetAsync(role, module, isAllowed);
        return Ok();
    }
}
