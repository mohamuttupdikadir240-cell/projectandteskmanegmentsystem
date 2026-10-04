using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstatePMS.Data;
using RealEstatePMS.Models.Enums;
using RealEstatePMS.ViewModels;

namespace RealEstatePMS.Controllers;

[AllowAnonymous]
public class PublicController : Controller
{
    private readonly ApplicationDbContext _context;

    public PublicController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        var vm = new PublicHomeViewModel
        {
            ProjectCount = await _context.Projects.CountAsync(),
            PropertyCount = await _context.Properties.CountAsync(),
            CustomerCount = await _context.Customers.CountAsync(),
        };

        return View(vm);
    }

    public async Task<IActionResult> Properties(string? search)
    {
        var query = _context.Properties
            .Include(p => p.Project)
            .Include(p => p.Images)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Code.Contains(search) || (p.UnitNumber != null && p.UnitNumber.Contains(search)));

        var vm = new PublicPropertiesViewModel
        {
            Search = search,
            AvailableProperties = await query.Where(p => p.Status == PropertyStatus.Available)
                .OrderByDescending(p => p.CreatedDate).ToListAsync(),
            RentedProperties = await query.Where(p => p.Status == PropertyStatus.Rented)
                .OrderByDescending(p => p.CreatedDate).ToListAsync(),
        };

        return View(vm);
    }
}
