using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
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

public class WorkflowIntegrationTests
{
    private sealed class Weather : IWeatherService
    {
        public Task<WeatherAssessmentResult> GetWeatherRiskAsync(string sourceZone, string destinationZone, CancellationToken cancellationToken = default)
            => Task.FromResult(new WeatherAssessmentResult { WeatherRisk = "low" });
    }
    private sealed class Fixture : IAsyncDisposable
    {
        public readonly SqliteConnection Connection = new("Data Source=:memory:");
        public SmartFleetDbContext Db = null!;
        public WorkflowOrchestrator Workflow = null!;
        public SafetyGuardAgent Safety = null!;
        public User Supervisor = new() { Name = "Supervisor", Email = "supervisor@test", Role = Role.Supervisor, PasswordHash = "test" };
        public async Task Initialize()
        {
            await Connection.OpenAsync();
            Db = NewContext(); await Db.Database.EnsureCreatedAsync();
            Db.Users.Add(Supervisor); await Db.SaveChangesAsync();
            Safety = new SafetyGuardAgent(Db, NullLogger<SafetyGuardAgent>.Instance);
            Workflow = new WorkflowOrchestrator(Db, new MissionPlannerAgent(NullLogger<MissionPlannerAgent>.Instance),
                new DispatchTelemetryAgent(new RoverRepository(Db), new Weather(), NullLogger<DispatchTelemetryAgent>.Instance),
                new MaintenanceMechanicAgent(new FailureCatalogRepository(Db), NullLogger<MaintenanceMechanicAgent>.Instance),
                Safety, new Weather(), new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Simulation:Enabled"]="true" }).Build());
        }
        public SmartFleetDbContext NewContext() => new(new DbContextOptionsBuilder<SmartFleetDbContext>().UseSqlite(Connection).Options);
        public async Task<DispatchRequest> Request()
        {
            var request = new DispatchRequest { OperatorId=Supervisor.Id, SourceZone="WarehouseA-DockA1", DestinationZone="WarehouseA-DockB3", CargoType="Standard", PreferredTimeWindow=DateTime.UtcNow };
            Db.DispatchRequests.Add(request); await Db.SaveChangesAsync(); return request;
        }
        public Task<WorkflowRun> Start(DispatchRequest request, string risk="low") => Workflow.StartAsync(request.Id,Supervisor.Id,"Supervisor",risk,default);
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await Connection.DisposeAsync(); }
    }

    [Fact]
    public async Task PipelinePersistsAllAgentEventsAndCompletesExactlyOnce()
    {
        await using var f=new Fixture();await f.Initialize();var request=await f.Request();
        var run=await f.Start(request);Assert.Equal("Executing",run.Status);
        Assert.Equal(run.Id,(await f.Start(request)).Id);
        Assert.Equal(1,await f.Db.Rovers.CountAsync(r=>r.CurrentMissionId==request.Id.ToString()));
        Assert.Equal(4,await f.Db.WorkflowExecutionLogs.Where(l=>l.WorkflowRunId==run.Id && l.AgentName!="MissionExecutor").Select(l=>l.AgentName).Distinct().CountAsync());
        run.Progress=.99;run.UpdatedAt=DateTime.UtcNow.AddSeconds(-2);await f.Db.SaveChangesAsync();
        await f.Workflow.TickAsync(default);await f.Workflow.TickAsync(default);
        Assert.Equal("Completed",run.Status);Assert.Equal(DispatchRequestStatus.Completed,request.Status);
        var rover=await f.Db.Rovers.SingleAsync(r=>r.Id==run.RoverId);
        Assert.Null(rover.CurrentMissionId);Assert.Equal(request.DestinationZone,rover.LocationZone);
        Assert.Equal(1,await f.Db.WorkflowExecutionLogs.CountAsync(l=>l.WorkflowRunId==run.Id && l.StepName=="DeliveryCompleted"));
    }

    [Fact]
    public async Task HumanPauseDoesNotMoveAndRejectReleasesReservation()
    {
        await using var f=new Fixture();await f.Initialize();var request=await f.Request();var run=await f.Start(request,"medium");
        Assert.Equal("AwaitingApproval",run.Status);await f.Workflow.TickAsync(default);Assert.Equal(0,run.Progress);
        var approval=await f.Db.ApprovalRequests.SingleAsync(a=>a.WorkflowRunId==run.Id);
        await f.Workflow.DecideAsync(approval.Id,f.Supervisor.Id,"Rejected","Unsafe conditions",default);
        Assert.Equal("Rejected",run.Status);Assert.Null((await f.Db.Rovers.SingleAsync(r=>r.Id==run.RoverId)).CurrentMissionId);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>f.Workflow.DecideAsync(approval.Id,f.Supervisor.Id,"Approved",null,default));
    }

    [Fact]
    public async Task ApprovalSurvivesNewContextAndCannotResumeExpiredReservation()
    {
        await using var f=new Fixture();await f.Initialize();var run=await f.Start(await f.Request(),"medium");
        await using(var reopened=f.NewContext()) Assert.Equal("AwaitingApproval",(await reopened.WorkflowRuns.SingleAsync(x=>x.Id==run.Id)).Status);
        run.ReservedUntil=DateTime.UtcNow.AddSeconds(-1);await f.Db.SaveChangesAsync();
        var approval=await f.Db.ApprovalRequests.SingleAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(()=>f.Workflow.DecideAsync(approval.Id,f.Supervisor.Id,"Approved",null,default));
        await f.Workflow.TickAsync(default);Assert.Equal("Failed",run.Status);Assert.Equal(ApprovalStatus.Rejected,approval.Status);
    }

    [Fact]
    public async Task BreakdownStopsActiveMissionAndPreventsReselection()
    {
        await using var f=new Fixture();await f.Initialize();var run=await f.Start(await f.Request());
        f.Db.BreakdownReports.Add(new BreakdownReport { RoverId=run.RoverId,ReportedById=f.Supervisor.Id,SymptomCategory="MotorOverheating",Description="motor overheating" });
        await f.Workflow.BlockRoverAsync(run.RoverId!.Value,default);await f.Db.SaveChangesAsync();
        Assert.Equal("Failed",run.Status);Assert.Equal(RoverStatus.Maintenance,(await f.Db.Rovers.SingleAsync(r=>r.Id==run.RoverId)).Status);
        var next=await f.Start(await f.Request());Assert.NotEqual(run.RoverId,next.RoverId);
    }

    [Fact]
    public async Task StaleTrackedCandidateCannotOverwriteAnotherReservation()
    {
        await using var f=new Fixture();await f.Initialize();var one=await f.Request();var two=await f.Request();
        var id=Guid.Parse("11111111-1111-1111-1111-111111111111");
        await using var stale=f.NewContext();await stale.Rovers.SingleAsync(r=>r.Id==id);
        Assert.NotNull(await new RoverRepository(f.Db).LockRoverForMissionAsync(id,one.Id.ToString()));
        Assert.Null(await new RoverRepository(stale).LockRoverForMissionAsync(id,two.Id.ToString()));
        Assert.Equal(one.Id.ToString(),(await f.Db.Rovers.AsNoTracking().SingleAsync(r=>r.Id==id)).CurrentMissionId);
    }

    [Theory]
    [InlineData("empty-plan")][InlineData("unknown-weather")][InlineData("unlocked")][InlineData("low-battery")]
    public async Task InvalidSafetyPrerequisitesAreHardRejected(string scenario)
    {
        await using var f=new Fixture();await f.Initialize();var input=f.Safety.CreateMockedInput("low-risk");
        if(scenario=="empty-plan")input.MissionPlanSummary.Plan.Clear();
        if(scenario=="unknown-weather")input.TelemetryResult.WeatherRisk="unavailable";
        if(scenario=="unlocked")input.TelemetryResult.Locked=false;
        if(scenario=="low-battery")input.TelemetryResult.BatteryOk=false;
        var result=await f.Safety.EvaluateSafetyAsync(input);Assert.Equal("AutoRejected",result.AutoOutcome);Assert.False(result.RequiresApproval);
    }

    [Fact]
    public async Task SameRunCannotProduceDuplicateApprovals()
    {
        await using var f=new Fixture();await f.Initialize();var input=f.Safety.CreateMockedInput("pending-approval");input.WorkflowRunId=Guid.NewGuid();
        await f.Safety.EvaluateSafetyAsync(input);await f.Safety.EvaluateSafetyAsync(input);
        Assert.Equal(1,await f.Db.ApprovalRequests.CountAsync());
    }
}
