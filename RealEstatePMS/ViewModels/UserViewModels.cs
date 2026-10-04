using System.ComponentModel.DataAnnotations;

namespace RealEstatePMS.ViewModels;

public class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class UserEditViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required, StringLength(150)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Required]
    [Display(Name = "Role")]
    public string SelectedRole { get; set; } = string.Empty;

    public string[] AllRoles { get; set; } = Array.Empty<string>();
}
