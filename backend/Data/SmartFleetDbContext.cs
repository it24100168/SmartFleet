using Microsoft.EntityFrameworkCore;
using SmartFleet.Backend.Models;
using SmartFleet.Backend.Models.Enums;

namespace SmartFleet.Backend.Data;

public class SmartFleetDbContext : DbContext
{
    public SmartFleetDbContext(DbContextOptions<SmartFleetDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<BreakdownReport> BreakdownReports => Set<BreakdownReport>();
    public DbSet<FailureCatalog> FailureCatalogs => Set<FailureCatalog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --------------------------------------------------
        // User Entity Configuration
        // --------------------------------------------------
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

        // --------------------------------------------------
        // BreakdownReport Entity Configuration
        // --------------------------------------------------
        modelBuilder.Entity<BreakdownReport>(entity =>
        {
            entity.ToTable("BreakdownReports");

            entity.HasKey(b => b.Id);

            entity.Property(b => b.SymptomCategory)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(b => b.Description)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(b => b.PhotoUrl)
                .HasMaxLength(512);

            entity.Property(b => b.ErrorCode)
                .HasMaxLength(32);

            entity.Property(b => b.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(32);

            entity.Property(b => b.DiagnosisResultJson)
                .HasColumnType("text");

            entity.Property(b => b.CreatedAt)
                .IsRequired();

            entity.Property(b => b.UpdatedAt)
                .IsRequired();

            entity.HasOne(b => b.ReportedBy)
                .WithMany()
                .HasForeignKey(b => b.ReportedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(b => b.Status);
            entity.HasIndex(b => b.CreatedAt);
        });

        // --------------------------------------------------
        // FailureCatalog Entity Configuration & Seed Data
        // --------------------------------------------------
        modelBuilder.Entity<FailureCatalog>(entity =>
        {
            entity.ToTable("FailureCatalogs");

            entity.HasKey(f => f.Id);

            entity.Property(f => f.SymptomKeyword)
                .IsRequired()
                .HasMaxLength(128);

            entity.Property(f => f.SymptomCategory)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(f => f.LikelyPart)
                .IsRequired()
                .HasMaxLength(128);

            entity.Property(f => f.Severity)
                .IsRequired()
                .HasMaxLength(32);

            entity.HasIndex(f => f.SymptomKeyword);

            // Seed realistic diagnostic failure patterns (matching docs/agent-contracts.md)
            entity.HasData(
                new FailureCatalog
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111101"),
                    SymptomKeyword = "motor overheating",
                    SymptomCategory = "MotorOverheating",
                    LikelyPart = "Drive Motor Unit",
                    EstimatedRepairHours = 2,
                    Severity = "High"
                },
                new FailureCatalog
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111102"),
                    SymptomKeyword = "wheel jam",
                    SymptomCategory = "WheelJam",
                    LikelyPart = "Wheel Bearing & Axle Assembly",
                    EstimatedRepairHours = 3,
                    Severity = "High"
                },
                new FailureCatalog
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111103"),
                    SymptomKeyword = "grinding noise",
                    SymptomCategory = "MotorOverheating",
                    LikelyPart = "Drive Motor Unit",
                    EstimatedRepairHours = 2,
                    Severity = "High"
                },
                new FailureCatalog
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111104"),
                    SymptomKeyword = "sensor fault",
                    SymptomCategory = "SensorFault",
                    LikelyPart = "Front LiDAR / Ultrasonic Array",
                    EstimatedRepairHours = 1,
                    Severity = "Medium"
                },
                new FailureCatalog
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111105"),
                    SymptomKeyword = "battery degradation",
                    SymptomCategory = "BatteryDegradation",
                    LikelyPart = "Lithium Iron Phosphate Battery Pack",
                    EstimatedRepairHours = 4,
                    Severity = "Critical"
                },
                new FailureCatalog
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111106"),
                    SymptomKeyword = "communication loss",
                    SymptomCategory = "SensorFault",
                    LikelyPart = "Telemetry Wi-Fi / UWB Module",
                    EstimatedRepairHours = 1,
                    Severity = "Low"
                },
                new FailureCatalog
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111107"),
                    SymptomKeyword = "brake failure",
                    SymptomCategory = "WheelJam",
                    LikelyPart = "Electromagnetic Brake Caliper",
                    EstimatedRepairHours = 3,
                    Severity = "Critical"
                },
                new FailureCatalog
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111108"),
                    SymptomKeyword = "steering lock",
                    SymptomCategory = "WheelJam",
                    LikelyPart = "Steering Actuator Servo",
                    EstimatedRepairHours = 2,
                    Severity = "High"
                },
                new FailureCatalog
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111109"),
                    SymptomKeyword = "overvoltage",
                    SymptomCategory = "BatteryDegradation",
                    LikelyPart = "Power Distribution Board (PDB)",
                    EstimatedRepairHours = 2,
                    Severity = "High"
                }
            );
        });
    }
}
