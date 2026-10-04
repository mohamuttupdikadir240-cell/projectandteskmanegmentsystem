using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using RealEstatePMS.Data;
using RealEstatePMS.Filters;
using RealEstatePMS.Helpers;
using RealEstatePMS.Models;
using RealEstatePMS.Models.Entities;
using RealEstatePMS.Models.Enums;
using RealEstatePMS.Services;

namespace RealEstatePMS.Controllers;

[Authorize]
[ModulePermission(Modules.Documents)]
public class DocumentsController : Controller
{
    private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png" };
    private const long MaxFileSize = 10 * 1024 * 1024;

    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly IWebHostEnvironment _environment;
    private readonly UserManager<ApplicationUser> _userManager;

    public DocumentsController(ApplicationDbContext context, IFileStorageService fileStorageService,
        IWebHostEnvironment environment, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _environment = environment;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? search, DocumentType? type, int pageIndex = 1)
    {
        var query = _context.Documents
            .Include(d => d.Project).Include(d => d.Property)
            .Include(d => d.Sale).Include(d => d.Rental).Include(d => d.UploadedBy)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d => d.Title.Contains(search));

        if (type.HasValue) query = query.Where(d => d.Type == type.Value);

        query = query.OrderByDescending(d => d.UploadedDate);

        ViewBag.Search = search;
        ViewBag.Type = type;

        var result = await PaginatedList<Document>.CreateAsync(query, pageIndex, 10);
        return View(result);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateDropdownsAsync();
        return View(new Document());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Document document, IFormFile file)
    {
        // FilePath/FileName are computed from the uploaded file below, not posted by the form.
        ModelState.Remove(nameof(Document.FilePath));
        ModelState.Remove(nameof(Document.FileName));

        if (file == null || file.Length == 0)
            ModelState.AddModelError(string.Empty, "Please choose a file to upload.");
        else if (!_fileStorageService.IsAllowedFile(file, AllowedExtensions, MaxFileSize))
            ModelState.AddModelError(string.Empty, "Invalid file type or the file exceeds the 10MB size limit.");

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return View(document);
        }

        document.FilePath = await _fileStorageService.SaveFileAsync(file!, "documents");
        document.FileName = file!.FileName;
        document.FileSize = file.Length;
        document.UploadedById = _userManager.GetUserId(User);
        document.UploadedDate = DateTime.UtcNow;

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Document uploaded successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Download(int id)
    {
        var document = await _context.Documents.FindAsync(id);
        if (document == null) return NotFound();

        var fullPath = Path.Combine(_environment.WebRootPath, document.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(fullPath)) return NotFound();

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(fullPath, out var contentType))
            contentType = "application/octet-stream";

        var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
        return File(bytes, contentType, document.FileName ?? Path.GetFileName(fullPath));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id);
        if (document == null) return NotFound();
        return View(document);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var document = await _context.Documents.FindAsync(id);
        if (document == null) return NotFound();

        _fileStorageService.DeleteFile(document.FilePath);
        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = "Document deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync()
    {
        ViewBag.Projects = new SelectList(await _context.Projects.OrderBy(p => p.Name).ToListAsync(), "Id", "Name");
        ViewBag.Properties = new SelectList(await _context.Properties.OrderBy(p => p.Code).ToListAsync(), "Id", "Code");
        ViewBag.Sales = new SelectList(
            await _context.Sales.Include(s => s.Customer).Include(s => s.Property)
                .Select(s => new { s.Id, Label = "Sale #" + s.Id + " - " + s.Customer!.Name + " (" + s.Property!.Code + ")" })
                .ToListAsync(), "Id", "Label");

        ViewBag.Rentals = new SelectList(
            await _context.Rentals.Include(r => r.Customer).Include(r => r.Property)
                .Select(r => new { r.Id, Label = "Rental #" + r.Id + " - " + r.Customer!.Name + " (" + r.Property!.Code + ")" })
                .ToListAsync(), "Id", "Label");
        ViewBag.Customers = new SelectList(await _context.Customers.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
    }
}
