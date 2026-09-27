using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Backend.Data;
using SmartFleet.Backend.Models;
using SmartFleet.Backend.Models.Enums;
using SmartFleet.Backend.Services;

namespace SmartFleet.Backend.Controllers;

[ApiController, Route("api/workflows"), Authorize]
public class WorkflowsController(SmartFleetDbContext db, WorkflowOrchestrator workflow) : ControllerBase
{
    [HttpGet("zones")]
    public IActionResult Zones() => Ok(WarehouseLayout.Zones);
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string Role => User.FindFirstValue(ClaimTypes.Role) ?? "Operator";

    [HttpGet("fleet")]
    public async Task<IActionResult> Fleet(CancellationToken ct)
    {
        await using var snapshot = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var runs = await db.WorkflowRuns.AsNoTracking().Include(x => x.DispatchRequest)
            .Where(x => Role != "Operator" || x.DispatchRequest!.OperatorId == Actor)
            .OrderByDescending(x => x.CreatedAt).Take(60).ToListAsync(ct);
        var rovers = await db.Rovers.AsNoTracking().OrderBy(x => x.Identifier).ToListAsync(ct);
        var positions = await db.WorkflowRuns.AsNoTracking().Include(x => x.DispatchRequest)
            .Where(x => x.RoverId != null && (x.Status == "Executing" || x.Status == "Failed"))
            .OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        WarehouseLayout.Point Position(Rover rover)
        {
            var run = positions.FirstOrDefault(x => x.RoverId == rover.Id);
            if (run != null && (rover.CurrentMissionId == run.DispatchRequestId.ToString() && run.Status == "Executing"
                || rover.Status == RoverStatus.Maintenance && rover.LocationZone == "StoppedOnRoute" && run.Progress > 0)
                && WarehouseLayout.IsKnown(run.StartZone) && WarehouseLayout.IsKnown(run.DispatchRequest!.SourceZone) && WarehouseLayout.IsKnown(run.DispatchRequest.DestinationZone))
                return WarehouseLayout.Position(WarehouseLayout.Route(run.StartZone, run.DispatchRequest!.SourceZone, run.DispatchRequest.DestinationZone), run.Progress);
            var zone = WarehouseLayout.Zones.FirstOrDefault(z => z.Id == rover.LocationZone);
            return new(zone?.X ?? 50, zone?.Y ?? 52);
        }
        return Ok(new { demo = workflow.Demo, databaseProvider = db.Database.IsNpgsql() ? "PostgreSQL" : "SQLite", serverTime = DateTime.UtcNow, zones = WarehouseLayout.Zones,
            rovers = rovers.Select(x => new { x.Id, x.Identifier, x.Status, x.BatteryPercentage, x.LocationZone, x.CurrentMissionId, position = Position(x) }).ToList(),
            runs = runs.Select(x => new { x.Id, x.DispatchRequestId, x.RoverId, x.StartZone, x.Status, x.Progress, x.IsDemo, x.WeatherRisk, x.FailureReason,
                x.ReservedUntil, x.CreatedAt, x.UpdatedAt, sourceZone = x.DispatchRequest!.SourceZone, destinationZone = x.DispatchRequest.DestinationZone, cargoType = x.DispatchRequest.CargoType }),
            breakdowns = await db.BreakdownReports.AsNoTracking().Where(x => x.Status != BreakdownStatus.Repaired)
                .Select(x => new { x.Id, x.RoverId, x.SymptomCategory, x.Status }).ToListAsync(ct) });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var run = await db.WorkflowRuns.AsNoTracking().Include(x => x.DispatchRequest).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (run == null) return NotFound();
        if (Role == "Operator" && run.DispatchRequest!.OperatorId != Actor) return Forbid();
        var logs = await db.WorkflowExecutionLogs.AsNoTracking().Where(x => x.WorkflowRunId == id).OrderBy(x => x.Timestamp).ToListAsync(ct);
        var approval = Role == "Supervisor" ? await db.ApprovalRequests.AsNoTracking().SingleOrDefaultAsync(x => x.WorkflowRunId == id, ct) : null;
        return Ok(new { run = new { run.Id, run.Status, run.PlanJson, run.FailureReason }, logs, approval });
    }

    [HttpPost("dispatch/{requestId:guid}/start"), Authorize(Roles = "Operator,Supervisor")]
    public async Task<IActionResult> Start(Guid requestId, [FromBody] StartOptions options, CancellationToken ct)
    {
        var run = await workflow.StartAsync(requestId, Actor, Role, options.WeatherRisk, ct);
        return Ok(new { run.Id, run.DispatchRequestId, run.Status });
    }

    [HttpPost("demo-mission"), Authorize(Roles = "Operator,Supervisor")]
    public async Task<IActionResult> DemoMission([FromBody] DemoMissionInput input, CancellationToken ct)
    {
        if (!workflow.Demo) return NotFound();
        if (!WarehouseLayout.Zones.Any(x => x.Id == input.SourceZone) || !WarehouseLayout.Zones.Any(x => x.Id == input.DestinationZone)
            || input.SourceZone == input.DestinationZone || input.WeatherRisk is not ("low" or "medium" or "high")) return BadRequest(new { message = "Choose different known zones and a valid scenario." });
        var request = new DispatchRequest { OperatorId = Actor, SourceZone = input.SourceZone, DestinationZone = input.DestinationZone,
            CargoType = input.CargoType, PreferredTimeWindow = DateTime.UtcNow };
        db.DispatchRequests.Add(request); await db.SaveChangesAsync(ct);
        var run = await workflow.StartAsync(request.Id, Actor, Role, input.WeatherRisk, ct);
        return Ok(new { run.Id, run.DispatchRequestId, run.Status });
    }

    [HttpPost("demo-rovers/{id:guid}/charge"), Authorize(Roles = "Supervisor")]
    public async Task<IActionResult> Charge(Guid id, CancellationToken ct)
    {
        if (!workflow.Demo) return NotFound();
        var affected = await db.Rovers.Where(x => x.Id == id && x.CurrentMissionId == null && (x.Status == RoverStatus.Idle || x.Status == RoverStatus.Charging))
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.BatteryPercentage, 100).SetProperty(x => x.Status, RoverStatus.Idle), ct);
        return affected == 1 ? Ok(new { message = "Demo charge completed." }) : Conflict(new { message = "Only an idle or charging rover can be charged." });
    }
    [HttpPost("demo-rovers/{id:guid}/recover"), Authorize(Roles = "Supervisor,Technician")]
    public async Task<IActionResult> Recover(Guid id, CancellationToken ct)
    {
        if (!workflow.Demo) return NotFound();
        var affected = await db.Rovers.Where(x => x.Id == id && x.CurrentMissionId == null && x.Status == RoverStatus.Maintenance)
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.LocationZone, "WarehouseA-MaintenanceArea").SetProperty(x => x.UpdatedAt, DateTime.UtcNow), ct);
        return affected == 1 ? Ok(new { message = "Simulated recovery crew transferred the stopped robot to maintenance. Repair is still required." })
            : Conflict(new { message = "Only a stopped robot in Maintenance can be recovered." });
    }
    public record StartOptions(string? WeatherRisk);
    public record DemoMissionInput([Required] string SourceZone, [Required] string DestinationZone, [Required, StringLength(64)] string CargoType, string WeatherRisk = "low");
}
