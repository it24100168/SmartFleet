using SmartFleet.Backend.Models.Enums;

namespace SmartFleet.Backend.DTOs.Auth;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;

    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public Role Role { get; set; }

    public DateTime ExpiresAt { get; set; }
}
