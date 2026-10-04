using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RealEstatePMS.Data;
using RealEstatePMS.Models;
using RealEstatePMS.Models.Entities;

namespace RealEstatePMS.Services;

public class PermissionService : IPermissionService
{
    private const string CacheKey = "RolePermissionMatrix";

    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;

    public PermissionService(ApplicationDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<bool> CanAccessAsync(ClaimsPrincipal user, string module)
    {
        if (user.IsInRole(Roles.Admin)) return true;

        var matrix = await GetMatrixAsync();

        foreach (var role in Modules.ConfigurableRoles)
        {
            if (user.IsInRole(role) && matrix.TryGetValue(role, out var modules) && modules.Contains(module))
                return true;
        }

        return false;
    }

    public async Task<Dictionary<string, HashSet<string>>> GetMatrixAsync()
    {
        if (_cache.TryGetValue(CacheKey, out Dictionary<string, HashSet<string>>? cached) && cached != null)
            return cached;

        var rows = await _context.RolePermissions.Where(p => p.IsAllowed).ToListAsync();

        var matrix = new Dictionary<string, HashSet<string>>();
        foreach (var row in rows)
        {
            if (!matrix.TryGetValue(row.Role, out var set))
            {
                set = new HashSet<string>();
                matrix[row.Role] = set;
            }
            set.Add(row.Module);
        }

        _cache.Set(CacheKey, matrix, TimeSpan.FromMinutes(5));
        return matrix;
    }

    public async Task SetAsync(string role, string module, bool isAllowed)
    {
        var row = await _context.RolePermissions.FirstOrDefaultAsync(p => p.Role == role && p.Module == module);
        if (row == null)
        {
            row = new RolePermission { Role = role, Module = module, IsAllowed = isAllowed };
            _context.RolePermissions.Add(row);
        }
        else
        {
            row.IsAllowed = isAllowed;
        }

        await _context.SaveChangesAsync();
        _cache.Remove(CacheKey);
    }
}
