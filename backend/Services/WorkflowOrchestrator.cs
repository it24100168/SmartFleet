using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Backend.Agents.DispatchTelemetryAgent;
using SmartFleet.Backend.Agents.DispatchTelemetryAgent.Contracts;
using SmartFleet.Backend.Agents.MaintenanceMechanicAgent;
using SmartFleet.Backend.Agents.MaintenanceMechanicAgent.DTOs;
using SmartFleet.Backend.Agents.MissionPlannerAgent;
using SmartFleet.Backend.Agents.SafetyGuardAgent;
using SmartFleet.Backend.Agents.SafetyGuardAgent.DTOs;
using SmartFleet.Backend.Data;
using SmartFleet.Backend.Models;
using SmartFleet.Backend.Models.Enums;
using SmartFleet.Backend.Services.Interfaces;

namespace SmartFleet.Backend.Services;

public class WorkflowOrchestrator(SmartFleetDbContext db, IMissionPlannerAgent planner,
    IDispatchTelemetryAgent telemetry, IMaintenanceMechanicAgent mechanic, ISafetyGuardAgent safety,
    IWeatherService weather, IConfiguration config)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public bool Demo => config.GetValue<bool>("Simulation:Enabled");

    public async Task<WorkflowRun> StartAsync(Guid requestId, Guid actorId, string role, string? demoWeather, CancellationToken ct)
    {
        var objective = await db.DispatchRequests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == requestId, ct)
            ?? throw new KeyNotFoundException("Dispatch request not found.");
        if (role == "Operator" && objective.OperatorId != actorId) throw new UnauthorizedAccessException("This dispatch belongs to another operator.");
        if (role is not ("Operator" or "Supervisor")) throw new UnauthorizedAccessException("Only operators and supervisors can start missions.");
        WarehouseLayout.ValidateRoute(objective.SourceZone, objective.DestinationZone);
        if (demoWeather != null && (!Demo || demoWeather is not ("low" or "medium" or "high")))
            throw new ArgumentException("Weather fixtures require demo mode and a low, medium or high value.");
        var assessment = Demo
            ? new WeatherAssessmentResult { WeatherRisk = demoWeather ?? "low", ConditionDescription = "Explicit demo fixture", IsSimulatedFallback = true }
            : await weather.GetWeatherRiskAsync(objective.SourceZone, objective.DestinationZone, ct);

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var request = await db.DispatchRequests.SingleAsync(x => x.Id == requestId, ct);
        var existing = await db.WorkflowRuns.Where(x => x.DispatchRequestId == requestId && x.Status != "Generated")
            .OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        if (existing != null && existing.Status is not ("Failed" or "Rejected" or "RevisionRequested" or "Queued")) return existing;
        if (Demo && existing?.Status == "Queued" && demoWeather == null) assessment.WeatherRisk = existing.WeatherRisk;
        // Concurrency token claims this request; duplicates cannot commit a second reservation.
        request.Status = DispatchRequestStatus.Planned;
        request.UpdatedAt = DateTime.UtcNow;
        var input = new MissionPlannerInput { DispatchRequestId = request.Id.ToString(), SourceZone = request.SourceZone,
            DestinationZone = request.DestinationZone, CargoType = request.CargoType, Priority = request.Priority,
            PreferredTimeWindow = request.PreferredTimeWindow.ToString("O") };
        var plan = existing?.Status == "Queued"
            ? JsonSerializer.Deserialize<MissionPlannerOutput>(existing.PlanJson, Json)!
            : await planner.GeneratePlanAsync(input, ct);
        var run = existing?.Status == "Queued" ? existing : new WorkflowRun { DispatchRequestId = requestId, ObjectiveJson = Serialize(input), PlanJson = Serialize(plan),
            Status = "Planning", IsDemo = Demo, WeatherRisk = assessment.WeatherRisk };
        if (run != existing) db.WorkflowRuns.Add(run);
        await db.SaveChangesAsync(ct);
        if (run != existing) Log(run, "MissionPlanning", "MissionPlannerAgent", input, plan, "Succeeded");

        // Normal orders are scheduled for their delivery target, with a short intake window.
        // Explicit demo scenarios run immediately, but still wait for a safe available rover.
        var now = DateTime.UtcNow;
        var pending = await db.DispatchRequests.AsNoTracking().Where(x => x.Status == DispatchRequestStatus.Pending && x.Id != requestId).ToListAsync(ct);
        var earlier = pending.Any(x => x.PreferredTimeWindow <= now.AddSeconds(40) &&
            (x.PreferredTimeWindow < request.PreferredTimeWindow || x.PreferredTimeWindow == request.PreferredTimeWindow && PriorityRank(x.Priority) > PriorityRank(request.Priority)));
        var available = await db.Rovers.AnyAsync(r => r.Status == RoverStatus.Idle && r.CurrentMissionId == null
            && r.BatteryPercentage >= Math.Max(40, WarehouseLayout.RequiredBattery(request.SourceZone, request.DestinationZone))
            && !db.BreakdownReports.Any(b => b.RoverId == r.Id && b.Status != BreakdownStatus.Repaired), ct);
        if (assessment.WeatherRisk is "low" or "medium" && ((!available) || (demoWeather == null &&
            (request.PreferredTimeWindow > now.AddSeconds(40) || request.CreatedAt > now.AddSeconds(-5) || earlier))))
        {
            run.Status = "Queued"; request.Status = DispatchRequestStatus.Pending;
            run.FailureReason = !available ? "Waiting for a safe, charged rover." : "Scheduled: departure approximately 40 seconds before the delivery target; equal targets use Critical > High > Medium > Low.";
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return run;
        }
        run.FailureReason = null; run.WeatherRisk = assessment.WeatherRisk;

        var telemetryInput = new DispatchTelemetryAgentInput { DispatchRequestId = requestId.ToString(), SourceZone = request.SourceZone,
            DestinationZone = request.DestinationZone, WeatherAssessment = assessment,
            PlanSteps = plan.Plan.Select(p => new PlanStepDto { StepNumber = p.StepNumber, StepName = p.StepName, Status = p.Status }).ToList() };
        var result = await telemetry.ExecuteAsync(telemetryInput, ct);
        Log(run, "Reservation", "DispatchTelemetryAgent", new { request.SourceZone, request.DestinationZone, weather = assessment }, result, result.Locked ? "Succeeded" : "Blocked");
        var rover = result.Locked ? await db.Rovers.SingleAsync(x => x.Identifier == result.SelectedRoverId, ct) : null;
        run.RoverId = rover?.Id;
        run.StartZone = rover?.LocationZone ?? request.SourceZone;
        request.RoverId = rover?.Id;
        run.ReservedUntil = rover == null ? null : DateTime.UtcNow.AddMinutes(10);

        // Real open reports are diagnosed, never a fabricated fault for a weather/battery failure.
        var reports = await db.BreakdownReports.Where(x => x.Status != BreakdownStatus.Repaired).OrderBy(x => x.CreatedAt).ToListAsync(ct);
        foreach (var report in reports)
        {
            var diagnosis = await DiagnoseAsync(report, ct);
            Log(run, "FaultDiagnosis", "MaintenanceMechanicAgent", new { report.Id, report.RoverId, report.SymptomCategory }, diagnosis, "ExcludedFromFleet");
        }
        if (reports.Count == 0) Log(run, "MaintenanceCheck", "MaintenanceMechanicAgent", new { openReports = 0 }, new { reason = "No open breakdown reports" }, "Skipped");
        var guardInput = new SafetyGuardInput { WorkflowRunId = run.Id, DispatchRequestId = requestId.ToString(), RoverId = rover?.Id.ToString() ?? "",
            MissionPlanSummary = new MissionPlanSummary { Plan = plan.Plan.Select(p => new PlanStepSummary { StepNumber = p.StepNumber, StepName = p.StepName, Status = "Completed" }).ToList() },
            TelemetryResult = new TelemetryResultSummary { BatteryOk = result.BatteryOk, Locked = result.Locked, WeatherRisk = result.WeatherRisk } };
        var decision = await safety.EvaluateSafetyAsync(guardInput, ct);
        if (decision.AutoOutcome == "AutoApproved" && rover != null)
            BeginExecution(run, request, rover);
        else if (decision.RequiresApproval && rover != null)
        { run.Status = "AwaitingApproval"; request.Status = DispatchRequestStatus.AwaitingApproval; run.CurrentStep = 4; }
        else
        { run.Status = "Failed"; run.FailureReason = result.Reason ?? decision.RiskReason; request.Status = DispatchRequestStatus.Failed; if (rover != null) Release(rover); }
        run.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return run;
    }

    public async Task<ApprovalRequest> DecideAsync(Guid approvalId, Guid? actorId, string decision, string? notes, CancellationToken ct)
    {
        if (actorId == null || !await db.Users.AnyAsync(x => x.Id == actorId && x.Role == Role.Supervisor, ct))
            throw new UnauthorizedAccessException("A valid supervisor is required.");
        // Network outside transaction. Reservations are revalidated inside it below.
        var approvalInfo = await db.ApprovalRequests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == approvalId, ct)
            ?? throw new KeyNotFoundException("Approval not found.");
        WeatherAssessmentResult? fresh = null;
        if (decision == "Approved" && !Demo && Guid.TryParse(approvalInfo.DispatchRequestId, out var id))
        {
            var requestInfo = await db.DispatchRequests.AsNoTracking().SingleAsync(x => x.Id == id, ct);
            fresh = await weather.GetWeatherRiskAsync(requestInfo.SourceZone, requestInfo.DestinationZone, ct);
        }
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var approval = await db.ApprovalRequests.Include(x => x.ReviewedBy).SingleAsync(x => x.Id == approvalId, ct);
        var target = Enum.Parse<ApprovalStatus>(decision);
        if (approval.Status == target) return approval;
        if (approval.Status != ApprovalStatus.Pending) throw new InvalidOperationException("This approval has already been decided.");
        var run = await db.WorkflowRuns.SingleOrDefaultAsync(x => x.Id == approval.WorkflowRunId, ct)
            ?? throw new InvalidOperationException("This approval is not connected to a workflow.");
        var request = await db.DispatchRequests.SingleAsync(x => x.Id == run.DispatchRequestId, ct);
        var rover = await db.Rovers.SingleOrDefaultAsync(x => x.Id == run.RoverId, ct);
        if (decision == "Approved")
        {
            if (run.Status != "AwaitingApproval" || run.ReservedUntil < DateTime.UtcNow || rover == null
                || rover.Status != RoverStatus.Reserved || rover.CurrentMissionId != request.Id.ToString()
                || rover.BatteryPercentage < Math.Max(40, WarehouseLayout.RequiredBattery(request.SourceZone, request.DestinationZone))
                || await db.BreakdownReports.AnyAsync(x => x.RoverId == rover.Id && x.Status != BreakdownStatus.Repaired, ct)
                || (fresh?.WeatherRisk ?? run.WeatherRisk) is not ("low" or "medium"))
                throw new InvalidOperationException("Reservation or safety conditions changed. Reject or revise this mission.");
            BeginExecution(run, request, rover);
        }
        else
        {
            if (rover != null && rover.CurrentMissionId == request.Id.ToString()) Release(rover);
            run.Status = decision == "Rejected" ? "Rejected" : "RevisionRequested";
            request.Status = decision == "Rejected" ? DispatchRequestStatus.Rejected : DispatchRequestStatus.RevisionRequested;
            run.ReservedUntil = null;
        }
        approval.Status = target; approval.ReviewedById = actorId; approval.ReviewNotes = notes; approval.UpdatedAt = DateTime.UtcNow;
        run.UpdatedAt = request.UpdatedAt = DateTime.UtcNow;
        Log(run, "SupervisorDecision", "Supervisor", new { decision, notes, actorId }, new { run.Status }, decision);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return approval;
    }

    public async Task<MaintenanceMechanicOutput> DiagnoseAsync(BreakdownReport report, CancellationToken ct)
    {
        var result = await mechanic.DiagnoseAsync(new MaintenanceMechanicInput { BreakdownReportId = report.Id.ToString(),
            SymptomCategory = report.SymptomCategory, Description = report.Description, ErrorCode = report.ErrorCode }, ct);
        report.DiagnosisResultJson = Serialize(result);
        if (report.Status == BreakdownStatus.Reported) report.Status = result.RecommendedAction == "ScheduleRepair" ? BreakdownStatus.ScheduledForRepair : BreakdownStatus.Diagnosing;
        report.UpdatedAt = DateTime.UtcNow;
        return result;
    }

    public async Task BlockRoverAsync(Guid roverId, CancellationToken ct)
    {
        var rover = await db.Rovers.SingleOrDefaultAsync(x => x.Id == roverId, ct) ?? throw new ArgumentException("Rover not found.");
        rover.Status = RoverStatus.Maintenance;
        var runs = await db.WorkflowRuns.Where(x => x.RoverId == roverId && (x.Status == "Executing" || x.Status == "AwaitingApproval")).ToListAsync(ct);
        foreach (var run in runs)
        {
            if (run.Status == "Executing" && run.Progress > 0) rover.LocationZone = "StoppedOnRoute";
            run.Status = "Failed"; run.FailureReason = "Mission stopped: rover breakdown reported."; run.UpdatedAt = DateTime.UtcNow;
            var request = await db.DispatchRequests.SingleAsync(x => x.Id == run.DispatchRequestId, ct);
            request.Status = DispatchRequestStatus.Failed; request.UpdatedAt = DateTime.UtcNow;
            foreach (var approval in await db.ApprovalRequests.Where(x => x.WorkflowRunId == run.Id && x.Status == ApprovalStatus.Pending).ToListAsync(ct))
            { approval.Status = ApprovalStatus.Rejected; approval.ReviewNotes = run.FailureReason; }
            Log(run, "EmergencyStop", "MaintenanceMechanicAgent", new { roverId }, new { run.FailureReason }, "Stopped");
        }
        rover.CurrentMissionId = null; rover.UpdatedAt = DateTime.UtcNow;
    }

    public async Task TickAsync(CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var now = DateTime.UtcNow;
        var runs = await db.WorkflowRuns.Where(x => x.Status == "Executing" || x.Status == "AwaitingApproval").ToListAsync(ct);
        foreach (var run in runs)
        {
            var rover = await db.Rovers.SingleOrDefaultAsync(x => x.Id == run.RoverId, ct);
            var request = await db.DispatchRequests.SingleAsync(x => x.Id == run.DispatchRequestId, ct);
            if (run.Status == "AwaitingApproval")
            {
                if (run.ReservedUntil > now) continue;
                run.Status = "Failed"; run.FailureReason = "Supervisor reservation expired.";
                request.Status = DispatchRequestStatus.Failed;
                if (rover != null && rover.CurrentMissionId == request.Id.ToString()) Release(rover);
                foreach (var approval in await db.ApprovalRequests.Where(x => x.WorkflowRunId == run.Id && x.Status == ApprovalStatus.Pending).ToListAsync(ct))
                { approval.Status = ApprovalStatus.Rejected; approval.ReviewNotes = run.FailureReason; }
                Log(run, "ReservationExpiry", "MissionExecutor", new { run.ReservedUntil }, new { run.FailureReason }, "Failed");
            }
            else if (rover == null || rover.Status != RoverStatus.Dispatched || rover.CurrentMissionId != request.Id.ToString()
                || await db.BreakdownReports.AnyAsync(x => x.RoverId == run.RoverId && x.Status != BreakdownStatus.Repaired, ct))
            {
                run.Status = "Failed"; run.FailureReason = "Rover is no longer safe or reserved for this mission.";
                request.Status = DispatchRequestStatus.Failed;
                Log(run, "ExecutionStopped", "MissionExecutor", new { run.RoverId }, new { run.FailureReason }, "Failed");
            }
            else
            {
                // Advance from persisted state. An offline server never silently drives a robot.
                run.Progress = Math.Min(1, run.Progress + Math.Min(2, Math.Max(0, (now-run.UpdatedAt).TotalSeconds))/40);
                if (run.Progress >= 1)
                {
                    run.Status = "Completed"; request.Status = DispatchRequestStatus.Completed;
                    rover.LocationZone = request.DestinationZone;
                    rover.BatteryPercentage = Math.Max(0, rover.BatteryPercentage - (WarehouseLayout.RequiredBattery(request.SourceZone, request.DestinationZone)-20));
                    Release(rover); if (rover.BatteryPercentage < 40) rover.Status = RoverStatus.Charging;
                    Log(run, "DeliveryCompleted", "MissionExecutor", new { request.DestinationZone }, new { rover.Identifier, rover.BatteryPercentage }, "Completed");
                }
            }
            run.UpdatedAt = request.UpdatedAt = now;
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public static int PriorityRank(string priority) => priority switch { "Critical" => 4, "High" => 3, "Medium" => 2, _ => 1 };
    public async Task SchedulePendingAsync(CancellationToken ct)
    {
        var pending = await db.DispatchRequests.AsNoTracking().Where(x => x.Status == DispatchRequestStatus.Pending
            && x.PreferredTimeWindow <= DateTime.UtcNow.AddSeconds(40) && x.CreatedAt <= DateTime.UtcNow.AddSeconds(-5)).ToListAsync(ct);
        foreach (var request in pending.OrderBy(x => x.PreferredTimeWindow).ThenByDescending(x => PriorityRank(x.Priority)).ThenBy(x => x.CreatedAt))
            await StartAsync(request.Id, request.OperatorId, "Operator", null, ct);
    }

    private void BeginExecution(WorkflowRun run, DispatchRequest request, Rover rover)
    {
        run.Status = "Executing"; run.CurrentStep = 5; run.ReservedUntil = null; run.UpdatedAt = DateTime.UtcNow;
        request.Status = DispatchRequestStatus.InTransit; rover.Status = RoverStatus.Dispatched; rover.UpdatedAt = DateTime.UtcNow;
        Log(run, "DeliveryStarted", "MissionExecutor", new { request.SourceZone, request.DestinationZone }, new { rover.Identifier, simulatedHardware = true }, "Executing");
    }
    private static void Release(Rover rover) { rover.Status = RoverStatus.Idle; rover.CurrentMissionId = null; rover.UpdatedAt = DateTime.UtcNow; }
    public void Log(WorkflowRun run, string step, string agent, object input, object output, string result) => db.WorkflowExecutionLogs.Add(new WorkflowExecutionLog
    { WorkflowRunId = run.Id, DispatchRequestId = run.DispatchRequestId.ToString(), StepName = step, AgentName = agent, InputJson = Serialize(input), OutputJson = Serialize(output), ValidationResult = result });
    public static string Serialize(object value) => JsonSerializer.Serialize(value, Json);
}
