using System.ComponentModel.DataAnnotations;

namespace RealEstatePMS.Models.Entities;

public class RolePermission
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string Role { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Module { get; set; } = string.Empty;

    public bool IsAllowed { get; set; }
}
