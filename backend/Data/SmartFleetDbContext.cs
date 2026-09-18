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
    public DbSet<DispatchRequest> DispatchRequests => Set<DispatchRequest>();
    public DbSet<WorkflowRun> WorkflowRuns => Set<WorkflowRun>();

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
    }
}
