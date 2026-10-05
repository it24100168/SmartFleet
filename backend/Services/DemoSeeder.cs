using Microsoft.EntityFrameworkCore;
using SmartFleet.Backend.Data;
using SmartFleet.Backend.Models;
using SmartFleet.Backend.Models.Enums;
namespace SmartFleet.Backend.Services;
public static class DemoSeeder
{
    public static async Task SeedAsync(SmartFleetDbContext db, IConfiguration config)
    {
        foreach (var role in Enum.GetValues<Role>())
        {
            var email = $"{role.ToString().ToLowerInvariant()}@demo.smartfleet";
            if (!await db.Users.AnyAsync(x => x.Email == email)) db.Users.Add(new User
            {
                Name = $"Demo {role}", Email = email, Role = role,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(config["Simulation:Password"] ?? "DemoFleet!2026")
            });
        }
        await db.SaveChangesAsync();
    }
}
