namespace RealEstatePMS.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;

    public FileStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    private string GetWebRoot() =>
        string.IsNullOrEmpty(_environment.WebRootPath)
            ? Path.Combine(AppContext.BaseDirectory, "wwwroot")
            : _environment.WebRootPath;

    public bool IsAllowedFile(IFormFile file, string[] allowedExtensions, long maxSizeBytes)
    {
        if (file.Length <= 0 || file.Length > maxSizeBytes) return false;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return allowedExtensions.Contains(ext);
    }

    public async Task<string> SaveFileAsync(IFormFile file, string subFolder)
    {
        var uploadsRoot = Path.Combine(GetWebRoot(), "uploads", subFolder);
        Directory.CreateDirectory(uploadsRoot);

        var safeExtension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{safeExtension}";
        var fullPath = Path.Combine(uploadsRoot, fileName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/{subFolder}/{fileName}".Replace("\\", "/");
    }

    public void DeleteFile(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        var fullPath = Path.Combine(GetWebRoot(), relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
