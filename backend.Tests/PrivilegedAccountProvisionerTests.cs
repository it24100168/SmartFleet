using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartFleet.Backend.Data;
using SmartFleet.Backend.Models;
using SmartFleet.Backend.Models.Enums;
using SmartFleet.Backend.Services;
using Xunit;

namespace SmartFleet.Backend.Tests;

public class PrivilegedAccountProvisionerTests
{
    private static SmartFleetDbContext Database() => new(new DbContextOptionsBuilder<SmartFleetDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static IConfiguration Settings(params (string Key, string Value)[] entries) => new ConfigurationBuilder()
        .AddInMemoryCollection(entries.ToDictionary(x => x.Key, x => (string?)x.Value)).Build();

    [Fact]
    public async Task Private_bootstrap_creates_hashed_roles_once_and_never_overwrites_them()
    {
        await using var db = Database();
        var settings = Settings(
            ("Provisioning:Supervisor:Name", "Assessment Supervisor"),
            ("Provisioning:Supervisor:Email", "SUPERVISOR@example.test"),
            ("Provisioning:Supervisor:Password", "unique-private-password-123"),
            ("Provisioning:Technician:Name", "Assessment Technician"),
            ("Provisioning:Technician:Email", "technician@example.test"),
            ("Provisioning:Technician:Password", "another-private-password-123"));

        await PrivilegedAccountProvisioner.SeedAsync(db, settings);
        await PrivilegedAccountProvisioner.SeedAsync(db, settings);

        var users = await db.Users.OrderBy(x => x.Role).ToListAsync();
        Assert.Equal(2, users.Count);
        Assert.Contains(users, x => x.Role == Role.Supervisor && x.Email == "supervisor@example.test"
            && BCrypt.Net.BCrypt.Verify("unique-private-password-123", x.PasswordHash));
        Assert.Contains(users, x => x.Role == Role.Technician && x.Email == "technician@example.test"
            && BCrypt.Net.BCrypt.Verify("another-private-password-123", x.PasswordHash));
        Assert.DoesNotContain(users, x => x.PasswordHash.Contains("private-password-123"));
    }

    [Fact]
    public async Task Incomplete_or_short_privileged_credentials_fail_closed()
    {
        await using var db = Database();
        var settings = Settings(
            ("Provisioning:Supervisor:Name", "Supervisor"),
            ("Provisioning:Supervisor:Email", "supervisor@example.test"),
            ("Provisioning:Supervisor:Password", "short"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => PrivilegedAccountProvisioner.SeedAsync(db, settings));
        Assert.Empty(await db.Users.ToListAsync());
    }

    [Fact]
    public async Task Existing_operator_cannot_be_promoted_by_bootstrap_configuration()
    {
        await using var db = Database();
        db.Users.Add(new User { Email = "shared@example.test", Name = "Operator", Role = Role.Operator, PasswordHash = "existing" });
        await db.SaveChangesAsync();
        var settings = Settings(
            ("Provisioning:Supervisor:Name", "Supervisor"),
            ("Provisioning:Supervisor:Email", "shared@example.test"),
            ("Provisioning:Supervisor:Password", "unique-private-password-123"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => PrivilegedAccountProvisioner.SeedAsync(db, settings));
        Assert.Equal(Role.Operator, (await db.Users.SingleAsync()).Role);
    }
}
