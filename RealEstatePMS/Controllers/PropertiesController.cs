using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RealEstatePMS.Data;
using RealEstatePMS.Filters;
using RealEstatePMS.Helpers;
using RealEstatePMS.Models;
using RealEstatePMS.Models.Entities;
using RealEstatePMS.Models.Enums;
using RealEstatePMS.Services;
using RealEstatePMS.ViewModels;

namespace RealEstatePMS.Controllers;

[Authorize]
[ModulePermission(Modules.Properties)]
public class PropertiesController : Controller
{
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxImageSize = 5 * 1024 * 1024;

    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;

    public PropertiesController(ApplicationDbContext context, IFileStorageService fileStorageService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
    }

    public async Task<IActionResult> Index(string? search, PropertyType? type, PropertyStatus? status, int? projectId, int pageIndex = 1)
    {
        var query = _context.Properties.Include(p => p.Project).Include(p => p.Images).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Code.Contains(search) || (p.UnitNumber != null && p.UnitNumber.Contains(search)));

        if (type.HasValue) query = query.Where(p => p.Type == type.Value);
        if (status.HasValue) query = query.Where(p => p.Status == status.Value);
        if (projectId.HasValue) query = query.Where(p => p.ProjectId == projectId.Value);

        query = query.OrderByDescending(p => p.CreatedDate);

        ViewBag.Search = search;
        ViewBag.Type = type;
        ViewBag.Status = status;
        ViewBag.ProjectId = projectId;
        ViewBag.Projects = new SelectList(await _context.Projects.OrderBy(p => p.Name).ToListAsync(), "Id", "Name", projectId);

        var result = await PaginatedList<Property>.CreateAsync(query, pageIndex, 8);
        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        var property = await _context.Properties
            .Include(p => p.Project)
            .Include(p => p.Images)
            .Include(p => p.Sale).ThenInclude(s => s!.Customer)
            .Include(p => p.Rental).ThenInclude(r => r!.Customer)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (property == null) return NotFound();
        return View(property);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateProjectsAsync();
        return View(new PropertyFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PropertyFormViewModel model)
    {
        if (await _context.Properties.AnyAsync(p => p.Code == model.Code))
            ModelState.AddModelError(nameof(model.Code), "This property code is already in use.");

        if (!ModelState.IsValid)
        {
            await PopulateProjectsAsync(model.ProjectId);
            return View(model);
        }

        var property = new Property
        {
            Code = model.Code,
            ProjectId = model.ProjectId,
            Type = model.Type,
            Building = model.Building,
            Floor = model.Floor,
            UnitNumber = model.UnitNumber,
            Area = model.Area,
            Bedrooms = model.Bedrooms,
            Bathrooms = model.Bathrooms,
            Price = model.Price,
            Status = model.Status,
            Description = model.Description
        };

        _context.Properties.Add(property);
        await _context.SaveChangesAsync();

        await SaveImagesAsync(property.Id, model.NewImages);

        TempData["StatusMessage"] = "Property created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var property = await _context.Properties.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
        if (property == null) return NotFound();

        await PopulateProjectsAsync(property.ProjectId);

        return View(new PropertyFormViewModel
        {
            Id = property.Id,
            Code = property.Code,
            ProjectId = property.ProjectId,
            Type = property.Type,
            Building = property.Building,
            Floor = property.Floor,
            UnitNumber = property.UnitNumber,
            Area = property.Area,
            Bedrooms = property.Bedrooms,
            Bathrooms = property.Bathrooms,
            Price = property.Price,
            Status = property.Status,
            Description = property.Description,
            ExistingImages = property.Images.ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PropertyFormViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (await _context.Properties.AnyAsync(p => p.Code == model.Code && p.Id != id))
            ModelState.AddModelError(nameof(model.Code), "This property code is already in use.");

        var property = await _context.Properties.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
        if (property == null) return NotFound();

        if (!ModelState.IsValid)
        {
            await PopulateProjectsAsync(model.ProjectId);
            model.ExistingImages = property.Images.ToList();
            return View(model);
        }

        property.Code = model.Code;
        property.ProjectId = model.ProjectId;
        property.Type = model.Type;
        property.Building = model.Building;
        property.Floor = model.Floor;
        property.UnitNumber = model.UnitNumber;
        property.Area = model.Area;
        property.Bedrooms = model.Bedrooms;
        property.Bathrooms = model.Bathrooms;
        property.Price = model.Price;
        property.Status = model.Status;
        property.Description = model.Description;

        await _context.SaveChangesAsync();
        await SaveImagesAsync(property.Id, model.NewImages);

        TempData["StatusMessage"] = "Property updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int imageId, int propertyId)
    {
        var image = await _context.PropertyImages.FindAsync(imageId);
        if (image != null)
        {
            _fileStorageService.DeleteFile(image.ImagePath);
            _context.PropertyImages.Remove(image);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Edit), new { id = propertyId });
    }

    public async Task<IActionResult> Delete(int id)
    {
        var property = await _context.Properties.Include(p => p.Project).FirstOrDefaultAsync(p => p.Id == id);
        if (property == null) return NotFound();
        return View(property);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var property = await _context.Properties
            .Include(p => p.Images)
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (property == null) return NotFound();

        if (property.Status is PropertyStatus.Sold or PropertyStatus.Rented)
        {
            TempData["ErrorMessage"] = "Cannot delete a property that has been sold or rented.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var image in property.Images)
            _fileStorageService.DeleteFile(image.ImagePath);

        _context.Documents.RemoveRange(property.Documents);
        _context.Properties.Remove(property);
        await _context.SaveChangesAsync();
        TempData["StatusMessage"] = "Property deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task SaveImagesAsync(int propertyId, List<IFormFile>? files)
    {
        if (files == null || !files.Any()) return;

        foreach (var file in files.Where(f => _fileStorageService.IsAllowedFile(f, AllowedImageExtensions, MaxImageSize)))
        {
            var path = await _fileStorageService.SaveFileAsync(file, "properties");
            _context.PropertyImages.Add(new PropertyImage { PropertyId = propertyId, ImagePath = path });
        }

        await _context.SaveChangesAsync();
    }

    private async Task PopulateProjectsAsync(int? selected = null)
    {
        ViewBag.ProjectsList = new SelectList(await _context.Projects.OrderBy(p => p.Name).ToListAsync(), "Id", "Name", selected);
    }
}
