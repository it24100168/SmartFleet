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
    public DbSet<DispatchRequest> DispatchRequests => Set<DispatchRequest>();
    public DbSet<WorkflowRun> WorkflowRuns => Set<WorkflowRun>();
    public DbSet<BreakdownReport> BreakdownReports => Set<BreakdownReport>();
    public DbSet<FailureCatalog> FailureCatalogs => Set<FailureCatalog>();

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

        modelBuilder.Entity<DispatchRequest>(entity =>
        {
            entity.ToTable("DispatchRequests");

            entity.HasKey(d => d.Id);

            entity.Property(d => d.SourceZone)
                .IsRequired()
                .HasMaxLength(128);

            entity.Property(d => d.DestinationZone)
                .IsRequired()
                .HasMaxLength(128);

            entity.Property(d => d.CargoType)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(d => d.Priority)
                .IsRequired()
                .HasMaxLength(32);

            entity.Property(d => d.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(32);

            entity.Property(d => d.PreferredTimeWindow)
                .IsRequired();

            entity.Property(d => d.CreatedAt)
                .IsRequired();

            entity.Property(d => d.UpdatedAt)
                .IsRequired();

            // Relationships
            entity.HasOne(d => d.Operator)
                .WithMany()
                .HasForeignKey(d => d.OperatorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(d => d.WorkflowRuns)
                .WithOne(w => w.DispatchRequest)
                .HasForeignKey(w => w.DispatchRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(d => d.OperatorId);
            entity.HasIndex(d => d.Status);
            entity.HasIndex(d => d.CreatedAt);
        });

        modelBuilder.Entity<WorkflowRun>(entity =>
        {
            entity.ToTable("WorkflowRuns");

            entity.HasKey(w => w.Id);

            entity.Property(w => w.ObjectiveJson)
                .IsRequired()
                .HasColumnType("text");

            entity.Property(w => w.PlanJson)
                .IsRequired()
                .HasColumnType("text");

            entity.Property(w => w.Status)
                .IsRequired()
                .HasMaxLength(32);

            entity.Property(w => w.CurrentStep)
                .IsRequired();

            entity.Property(w => w.CreatedAt)
                .IsRequired();

            entity.Property(w => w.UpdatedAt)
                .IsRequired();

            entity.HasIndex(w => w.DispatchRequestId);
        });

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
