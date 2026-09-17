using SmartFleet.Backend.Models.Enums;

namespace SmartFleet.Backend.Models;

/// <summary>
/// Represents an authenticated system user (Operator, Technician, or Supervisor).
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public Role Role { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
