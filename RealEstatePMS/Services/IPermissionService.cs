using System.Security.Claims;

namespace RealEstatePMS.Services;

public interface IPermissionService
{
    /// <summary>True if any role held by the user grants access to the module (Admin always does).</summary>
    Task<bool> CanAccessAsync(ClaimsPrincipal user, string module);

    /// <summary>Role -> set of module keys that role is allowed to access.</summary>
    Task<Dictionary<string, HashSet<string>>> GetMatrixAsync();

    Task SetAsync(string role, string module, bool isAllowed);
}
