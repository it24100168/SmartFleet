using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using SmartFleet.Backend.Agents.DispatchTelemetryAgent;
using SmartFleet.Backend.Agents.MaintenanceMechanicAgent;
using SmartFleet.Backend.Agents.MissionPlannerAgent;
using SmartFleet.Backend.Agents.SafetyGuardAgent;
using SmartFleet.Backend.Data;
using SmartFleet.Backend.Data.Repositories;
using SmartFleet.Backend.Models;
using SmartFleet.Backend.Models.Enums;
using SmartFleet.Backend.Services;
using SmartFleet.Backend.Services.Interfaces;
using Xunit;

namespace SmartFleet.Backend.Tests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SMARTFLEET_TEST_POSTGRES")))
            Skip = "Set SMARTFLEET_TEST_POSTGRES to the isolated local PostgreSQL instance.";
    }
}

// Every test creates its own random database. No existing database is reset or deleted.
public class PostgresWorkflowTests
{
    private sealed class Database : IAsyncDisposable
    {
        private readonly string name = "smartfleet_test_" + Guid.NewGuid().ToString("N");
        private string admin = "", connection = "";
        public readonly IConfiguration Config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string,string?> { ["Simulation:Enabled"] = "true" }).Build();
        public async Task Initialize()
        {
            var settings = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("SMARTFLEET_TEST_POSTGRES"));
            if (settings.Host is not ("localhost" or "127.0.0.1")) throw new InvalidOperationException("Integration tests require isolated localhost PostgreSQL.");
            settings.Database = "postgres"; settings.Pooling = false; admin = settings.ConnectionString;
            await using var c = new NpgsqlConnection(admin); await c.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", c); await command.ExecuteNonQueryAsync();
            settings.Database = name; connection = settings.ConnectionString;
            await using var db = Open(); await db.Database.MigrateAsync(); await DemoSeeder.SeedAsync(db, Config);
        }
        public SmartFleetDbContext Open() => new(new DbContextOptionsBuilder<SmartFleetDbContext>().UseNpgsql(connection).Options);
        public WorkflowOrchestrator Workflow(SmartFleetDbContext db) => new(db,
            new MissionPlannerAgent(NullLogger<MissionPlannerAgent>.Instance),
            new DispatchTelemetryAgent(new RoverRepository(db), new Weather(), NullLogger<DispatchTelemetryAgent>.Instance),
            new MaintenanceMechanicAgent(new FailureCatalogRepository(db), NullLogger<MaintenanceMechanicAgent>.Instance),
            new SafetyGuardAgent(db, NullLogger<SafetyGuardAgent>.Instance), new Weather(), Config);
        public async Task<DispatchRequest> Request(SmartFleetDbContext db, string priority = "Critical")
        {
            var owner = await db.Users.SingleAsync(x => x.Role == Role.Operator);
            var request = new DispatchRequest { OperatorId=owner.Id, SourceZone="WarehouseA-DockA1", DestinationZone="WarehouseA-DockB3",
                CargoType="Standard", Priority=priority, PreferredTimeWindow=DateTime.UtcNow, CreatedAt=DateTime.UtcNow.AddSeconds(-10) };
            db.DispatchRequests.Add(request); await db.SaveChangesAsync(); return request;
        }
        public async ValueTask DisposeAsync()
        {
            if (string.IsNullOrEmpty(admin)) return;
            await using var c=new NpgsqlConnection(admin);await c.OpenAsync();
            await using var cmd=new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)",c);await cmd.ExecuteNonQueryAsync();
        }
    }
    private sealed class Weather : IWeatherService
    {
        public Task<WeatherAssessmentResult> GetWeatherRiskAsync(string sourceZone,string destinationZone,CancellationToken cancellationToken=default)
            => Task.FromResult(new WeatherAssessmentResult { WeatherRisk="low" });
    }

    [PostgresFact]
    public async Task CriticalApprovalSurvivesContextRestartAndCompletesOnce()
    {
        await using var fixture=new Database();await fixture.Initialize();
        Guid runId, requestId, approvalId, operatorId;
        await using(var db=fixture.Open())
        {
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            var request=await fixture.Request(db);requestId=request.Id;operatorId=request.OperatorId;
            var run=await fixture.Workflow(db).StartAsync(request.Id,operatorId,"Operator",null,default);runId=run.Id;
            Assert.Equal("AwaitingApproval",run.Status);Assert.Equal(0,run.Progress);
            await fixture.Workflow(db).TickAsync(default);Assert.Equal(0,run.Progress);
            var approval=await db.ApprovalRequests.SingleAsync(x=>x.WorkflowRunId==runId);approvalId=approval.Id;
            Assert.Contains("Critical-priority",approval.RiskReason);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>fixture.Workflow(db).DecideAsync(approvalId,operatorId,"Approved",null,default));
        }
        await using(var db=fixture.Open())
        {
            var supervisor=await db.Users.SingleAsync(x=>x.Role==Role.Supervisor);
            var workflow=fixture.Workflow(db);
            await workflow.DecideAsync(approvalId,supervisor.Id,"Approved","Confirmed urgent dispatch",default);
            await workflow.DecideAsync(approvalId,supervisor.Id,"Approved","Duplicate retry",default);
            var run=await db.WorkflowRuns.SingleAsync(x=>x.Id==runId);Assert.Equal("Executing",run.Status);
            run.Progress=.99;run.UpdatedAt=DateTime.UtcNow.AddSeconds(-2);await db.SaveChangesAsync();
            await workflow.TickAsync(default);await workflow.TickAsync(default);
            Assert.Equal(DispatchRequestStatus.Completed,(await db.DispatchRequests.SingleAsync(x=>x.Id==requestId)).Status);
            Assert.Equal(1,await db.WorkflowExecutionLogs.CountAsync(x=>x.WorkflowRunId==runId&&x.StepName=="DeliveryCompleted"));
        }
    }

    [PostgresFact]
    public async Task ConcurrentReservationsHaveExactlyOneWinner()
    {
        await using var fixture=new Database();await fixture.Initialize();
        Guid roverId, first, second;
        await using(var db=fixture.Open())
        {
            first=(await fixture.Request(db)).Id;second=(await fixture.Request(db)).Id;
            roverId=(await db.Rovers.FirstAsync(x=>x.Status==RoverStatus.Idle&&x.BatteryPercentage>=80)).Id;
        }
        async Task<bool> Reserve(Guid request)
        {
            await using var db=fixture.Open();return await new RoverRepository(db).LockRoverForMissionAsync(roverId,request.ToString())!=null;
        }
        var outcomes=await Task.WhenAll(Reserve(first),Reserve(second));Assert.Single(outcomes.Where(x=>x));
    }

    [PostgresFact]
    public async Task MigrationsEnforceUniquenessAndTransactionsRollback()
    {
        await using var fixture=new Database();await fixture.Initialize();await using var db=fixture.Open();
        var count=await db.DispatchRequests.CountAsync();
        await using(var tx=await db.Database.BeginTransactionAsync()) { await fixture.Request(db); await tx.RollbackAsync(); }
        db.ChangeTracker.Clear();Assert.Equal(count,await db.DispatchRequests.CountAsync());
        var email=(await db.Users.FirstAsync()).Email;
        db.Users.Add(new User { Email=email,Name="Duplicate",PasswordHash="test",Role=Role.Operator });
        await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());
    }

    [PostgresFact]
    public async Task CriticalPriorityCannotOverrideUnsafeWeather()
    {
        await using var fixture=new Database();await fixture.Initialize();await using var db=fixture.Open();
        var request=await fixture.Request(db);
        var run=await fixture.Workflow(db).StartAsync(request.Id,request.OperatorId,"Operator","high",default);
        Assert.Equal("Failed",run.Status);Assert.Empty(await db.ApprovalRequests.ToListAsync());
        Assert.False(await db.Rovers.AnyAsync(x=>x.CurrentMissionId==request.Id.ToString()));
    }
}
