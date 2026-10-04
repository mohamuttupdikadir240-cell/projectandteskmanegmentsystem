using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RealEstatePMS.Data;
using RealEstatePMS.Filters;
using RealEstatePMS.Helpers;
using RealEstatePMS.Models;
using RealEstatePMS.Models.Entities;
using RealEstatePMS.Models.Enums;

namespace RealEstatePMS.Controllers;

[Authorize]
[ModulePermission(Modules.Projects)]
public class ProjectsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProjectsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? search, ProjectStatus? status, int pageIndex = 1)
    {
        var query = _context.Projects.Include(p => p.ProjectManager).Include(p => p.Properties).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || p.Code.Contains(search) || p.Location.Contains(search));

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        query = query.OrderByDescending(p => p.CreatedDate);

        ViewBag.Search = search;
        ViewBag.Status = status;

        var result = await PaginatedList<Project>.CreateAsync(query, pageIndex, 5);
        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        var project = await _context.Projects
            .Include(p => p.ProjectManager)
            .Include(p => p.Properties)
            .Include(p => p.Tasks).ThenInclude(t => t.AssignedEmployee)
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null) return NotFound();

        ViewBag.StatusHistory = await _context.ProjectStatusHistories
            .Include(h => h.ChangedBy)
            .Where(h => h.ProjectId == id)
            .OrderByDescending(h => h.ChangedDate)
            .ToListAsync();

        return View(project);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateManagersAsync();
        return View(new Project { StartDate = DateTime.Today, ExpectedEndDate = DateTime.Today.AddMonths(6) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Project project)
    {
        if (await _context.Projects.AnyAsync(p => p.Code == project.Code))
            ModelState.AddModelError(nameof(project.Code), "This project code is already in use.");

        if (!ModelState.IsValid)
        {
            await PopulateManagersAsync(project.ProjectManagerId);
            return View(project);
        }

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        TempData["StatusMessage"] = "Project created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null) return NotFound();
        await PopulateManagersAsync(project.ProjectManagerId);
        return View(project);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Project project, string? changeReason)
    {
        if (id != project.Id) return NotFound();

        if (await _context.Projects.AnyAsync(p => p.Code == project.Code && p.Id != id))
            ModelState.AddModelError(nameof(project.Code), "This project code is already in use.");

        var existing = await _context.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (existing == null) return NotFound();

        bool statusOrProgressChanged = existing.Status != project.Status || existing.ProgressPercentage != project.ProgressPercentage;
        if (statusOrProgressChanged && string.IsNullOrWhiteSpace(changeReason))
            ModelState.AddModelError(nameof(changeReason), "Please explain why the status/progress is changing.");

        if (!ModelState.IsValid)
        {
            await PopulateManagersAsync(project.ProjectManagerId);
            ViewBag.ChangeReason = changeReason;
            return View(project);
        }

        try
        {
            _context.Update(project);

            if (statusOrProgressChanged)
            {
                _context.ProjectStatusHistories.Add(new ProjectStatusHistory
                {
                    ProjectId = id,
                    OldStatus = existing.Status,
                    NewStatus = project.Status,
                    OldProgress = existing.ProgressPercentage,
                    NewProgress = project.ProgressPercentage,
                    Reason = changeReason,
                    ChangedById = _userManager.GetUserId(User)
                });
            }

            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Projects.AnyAsync(p => p.Id == id)) return NotFound();
            throw;
        }

        TempData["StatusMessage"] = "Project updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var project = await _context.Projects.Include(p => p.ProjectManager).FirstOrDefaultAsync(p => p.Id == id);
        if (project == null) return NotFound();
        return View(project);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var project = await _context.Projects
            .Include(p => p.Properties).ThenInclude(p => p.Documents)
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (project == null) return NotFound();

        if (project.Properties.Any(p => p.Status == PropertyStatus.Sold || p.Status == PropertyStatus.Rented))
        {
            TempData["ErrorMessage"] = "Cannot delete a project that has sold or rented properties.";
            return RedirectToAction(nameof(Index));
        }

        _context.Documents.RemoveRange(project.Documents);
        foreach (var property in project.Properties)
        {
            _context.Documents.RemoveRange(property.Documents);
        }
        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
        TempData["StatusMessage"] = "Project deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateManagersAsync(string? selected = null)
    {
        var managers = await _userManager.GetUsersInRoleAsync(Roles.ProjectManager);
        ViewBag.Managers = new SelectList(managers, "Id", "FullName", selected);
    }
}
