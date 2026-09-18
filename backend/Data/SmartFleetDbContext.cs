using Microsoft.EntityFrameworkCore;
using SmartFleet.Backend.Models;

namespace SmartFleet.Backend.Data;

public class SmartFleetDbContext : DbContext
{
    public SmartFleetDbContext(DbContextOptions<SmartFleetDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Rover> Rovers => Set<Rover>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(u => u.Id);

            entity.Property(u => u.Name)
                .IsRequired()
                .HasMaxLength(128);

            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(256);

            entity.HasIndex(u => u.Email)
                .IsUnique();

            entity.Property(u => u.PasswordHash)
                .IsRequired();

            entity.Property(u => u.Role)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(32);

            entity.Property(u => u.CreatedAt)
                .IsRequired();

            entity.Property(u => u.UpdatedAt)
                .IsRequired();
        });

        modelBuilder.Entity<Rover>(entity =>
        {
            entity.ToTable("Rovers");

            entity.HasKey(r => r.Id);

            entity.Property(r => r.Identifier)
                .IsRequired()
                .HasMaxLength(32);

            entity.HasIndex(r => r.Identifier)
                .IsUnique();

            entity.Property(r => r.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(32);

            entity.Property(r => r.BatteryPercentage)
                .IsRequired();

            entity.Property(r => r.LocationZone)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(r => r.CurrentMissionId)
                .HasMaxLength(64);

            entity.Property(r => r.Version)
                .IsRowVersion();

            // Seed initial fleet rovers (including RO-04 matching docs/agent-contracts.md)
            var seedTime = new DateTime(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc);
            entity.HasData(
                new Rover
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Identifier = "RO-01",
                    Status = Models.Enums.RoverStatus.Idle,
                    BatteryPercentage = 95,
                    LocationZone = "WarehouseA-DockA1",
                    CurrentMissionId = null,
                    CreatedAt = seedTime,
                    UpdatedAt = seedTime
                },
                new Rover
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Identifier = "RO-02",
                    Status = Models.Enums.RoverStatus.Charging,
                    BatteryPercentage = 35,
                    LocationZone = "WarehouseA-ChargingBay",
                    CurrentMissionId = null,
                    CreatedAt = seedTime,
                    UpdatedAt = seedTime
                },
                new Rover
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Identifier = "RO-03",
                    Status = Models.Enums.RoverStatus.Dispatched,
                    BatteryPercentage = 60,
                    LocationZone = "WarehouseA-Aisle4",
                    CurrentMissionId = "d1a0-554b",
                    CreatedAt = seedTime,
                    UpdatedAt = seedTime
                },
                new Rover
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Identifier = "RO-04",
                    Status = Models.Enums.RoverStatus.Idle,
                    BatteryPercentage = 88,
                    LocationZone = "WarehouseA-DockA1",
                    CurrentMissionId = null,
                    CreatedAt = seedTime,
                    UpdatedAt = seedTime
                },
                new Rover
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Identifier = "RO-05",
                    Status = Models.Enums.RoverStatus.Faulted,
                    BatteryPercentage = 15,
                    LocationZone = "WarehouseA-MaintenanceArea",
                    CurrentMissionId = null,
                    CreatedAt = seedTime,
                    UpdatedAt = seedTime
                },
                new Rover
                {
                    Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    Identifier = "RO-06",
                    Status = Models.Enums.RoverStatus.Idle,
                    BatteryPercentage = 22,
                    LocationZone = "WarehouseA-DockB3",
                    CurrentMissionId = null,
                    CreatedAt = seedTime,
                    UpdatedAt = seedTime
                }
            );
        });
    }
}
