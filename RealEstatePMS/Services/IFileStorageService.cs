namespace RealEstatePMS.Services;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file, string subFolder);
    void DeleteFile(string relativePath);
    bool IsAllowedFile(IFormFile file, string[] allowedExtensions, long maxSizeBytes);
}
