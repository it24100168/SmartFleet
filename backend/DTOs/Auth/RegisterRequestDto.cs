using System.ComponentModel.DataAnnotations;
using SmartFleet.Backend.Models.Enums;

namespace SmartFleet.Backend.DTOs.Auth;

public class RegisterRequestDto
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(128, ErrorMessage = "Name cannot exceed 128 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required")]
    public Role Role { get; set; }
}
