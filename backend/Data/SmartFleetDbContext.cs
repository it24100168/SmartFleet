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
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<WorkflowExecutionLog> WorkflowExecutionLogs => Set<WorkflowExecutionLog>();

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

        modelBuilder.Entity<ApprovalRequest>(entity =>
        {
            entity.ToTable("ApprovalRequests");

            entity.HasKey(a => a.Id);

            entity.Property(a => a.DispatchRequestId)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(a => a.RoverId)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(a => a.AgentSummaryJson)
                .IsRequired();

            entity.Property(a => a.RiskScore)
                .IsRequired();

            entity.Property(a => a.RiskReason)
                .IsRequired()
                .HasMaxLength(512);

            entity.Property(a => a.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(32);

            entity.Property(a => a.ReviewNotes)
                .HasMaxLength(1000);

            entity.Property(a => a.CreatedAt)
                .IsRequired();

            entity.Property(a => a.UpdatedAt)
                .IsRequired();

            entity.HasIndex(a => a.DispatchRequestId);
            entity.HasIndex(a => a.Status);
            entity.HasIndex(a => a.CreatedAt);

            entity.HasOne(a => a.ReviewedBy)
                .WithMany()
                .HasForeignKey(a => a.ReviewedById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WorkflowExecutionLog>(entity =>
        {
            entity.ToTable("WorkflowExecutionLogs");

            entity.HasKey(w => w.Id);

            entity.Property(w => w.DispatchRequestId)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(w => w.StepName)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(w => w.AgentName)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(w => w.InputJson)
                .IsRequired();

            entity.Property(w => w.OutputJson)
                .IsRequired();

            entity.Property(w => w.ValidationResult)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(w => w.Timestamp)
                .IsRequired();

            entity.HasIndex(w => w.DispatchRequestId);
            entity.HasIndex(w => w.Timestamp);
        });
    }
}
