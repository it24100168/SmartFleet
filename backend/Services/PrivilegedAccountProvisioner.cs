using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Backend.Data;
using SmartFleet.Backend.Models;
using SmartFleet.Backend.Models.Enums;

namespace SmartFleet.Backend.Services;

/// <summary>
/// Creates initial privileged accounts only from private server configuration.
/// Public registration always remains Operator-only.
/// </summary>
public static class PrivilegedAccountProvisioner
{
    public static async Task SeedAsync(SmartFleetDbContext db, IConfiguration configuration, CancellationToken ct = default)
    {
        await SeedRoleAsync(db, configuration, Role.Supervisor, ct);
        await SeedRoleAsync(db, configuration, Role.Technician, ct);
    }

    private static async Task SeedRoleAsync(SmartFleetDbContext db, IConfiguration configuration, Role role, CancellationToken ct)
    {
        var prefix = $"Provisioning:{role}";
        var rawEmail = configuration[$"{prefix}:Email"];
        var password = configuration[$"{prefix}:Password"];
        var name = configuration[$"{prefix}:Name"];
        if (string.IsNullOrWhiteSpace(rawEmail) && string.IsNullOrWhiteSpace(password) && string.IsNullOrWhiteSpace(name)) return;

        if (string.IsNullOrWhiteSpace(rawEmail) || string.IsNullOrWhiteSpace(password)
            || string.IsNullOrWhiteSpace(name) || name.Length > 128 || password.Length < 12)
            throw new InvalidOperationException($"{prefix} requires Name, Email and a password of at least 12 characters in private server configuration.");

        var email = rawEmail.Trim().ToLowerInvariant();
        if (email.Length > 256 || !new EmailAddressAttribute().IsValid(email))
            throw new InvalidOperationException($"{prefix}:Email is invalid.");

        var existing = await db.Users.SingleOrDefaultAsync(x => x.Email == email, ct);
        if (existing != null)
        {
            if (existing.Role != role)
                throw new InvalidOperationException($"{prefix}:Email already belongs to another role.");
            return;
        }

        db.Users.Add(new User
        {
            Name = name.Trim(),
            Email = email,
            Role = role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        });
        await db.SaveChangesAsync(ct);
    }
}
