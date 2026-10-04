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
[ModulePermission(Modules.Tasks)]
[Route("Tasks")]
[Route("ProjectTasks")]
public class ProjectTasksController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProjectTasksController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, ProjectTaskStatus? status, int? projectId, int pageIndex = 1)
    {
        var query = _context.ProjectTasks.Include(t => t.Project).Include(t => t.AssignedEmployee).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Name.Contains(search));

        if (status.HasValue) query = query.Where(t => t.Status == status.Value);
        if (projectId.HasValue) query = query.Where(t => t.ProjectId == projectId.Value);

        query = query.OrderByDescending(t => t.CreatedDate);

        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.ProjectId = projectId;
        ViewBag.Projects = new SelectList(await _context.Projects.OrderBy(p => p.Name).ToListAsync(), "Id", "Name", projectId);

        var result = await PaginatedList<ProjectTask>.CreateAsync(query, pageIndex, 10);
        return View(result);
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        await PopulateDropdownsAsync();
        return View(new ProjectTask { StartDate = DateTime.Today, DueDate = DateTime.Today.AddDays(14) });
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectTask task)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(task.ProjectId, task.AssignedEmployeeId);
            return View(task);
        }

        _context.ProjectTasks.Add(task);
        await _context.SaveChangesAsync();
        TempData["StatusMessage"] = "Task created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var task = await _context.ProjectTasks.FindAsync(id);
        if (task == null) return NotFound();
        await PopulateDropdownsAsync(task.ProjectId, task.AssignedEmployeeId);
        return View(task);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProjectTask task)
    {
        if (id != task.Id) return NotFound();
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(task.ProjectId, task.AssignedEmployeeId);
            return View(task);
        }

        if (task.Progress >= 100) task.Status = ProjectTaskStatus.Completed;
        else if (task.DueDate < DateTime.Today && task.Status != ProjectTaskStatus.Completed) task.Status = ProjectTaskStatus.Delayed;

        try
        {
            _context.Update(task);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.ProjectTasks.AnyAsync(t => t.Id == id)) return NotFound();
            throw;
        }

        TempData["StatusMessage"] = "Task updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var task = await _context.ProjectTasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == id);
        if (task == null) return NotFound();
        return View(task);
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var task = await _context.ProjectTasks.FindAsync(id);
        if (task == null) return NotFound();

        _context.ProjectTasks.Remove(task);
        await _context.SaveChangesAsync();
        TempData["StatusMessage"] = "Task deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync(int? projectId = null, string? employeeId = null)
    {
        ViewBag.ProjectsList = new SelectList(await _context.Projects.OrderBy(p => p.Name).ToListAsync(), "Id", "Name", projectId);

        var employees = (await _userManager.GetUsersInRoleAsync(Roles.ProjectManager))
            .Concat(await _userManager.GetUsersInRoleAsync(Roles.SalesAgent))
            .Concat(await _userManager.GetUsersInRoleAsync(Roles.Accountant))
            .DistinctBy(u => u.Id);

        ViewBag.Employees = new SelectList(employees, "Id", "FullName", employeeId);
    }
}
