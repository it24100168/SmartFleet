using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Backend.Agents.SafetyGuardAgent.DTOs;
using SmartFleet.Backend.Data;
using SmartFleet.Backend.DTOs.Approvals;
using SmartFleet.Backend.Models;
using SmartFleet.Backend.Models.Enums;
using SmartFleet.Backend.Services.Interfaces;

namespace SmartFleet.Backend.Services;

public class ApprovalService : IApprovalService
{
    private readonly SmartFleetDbContext _dbContext;
    private readonly IDispatchSyncService _dispatchSyncService;
    private readonly ILogger<ApprovalService> _logger;

    public ApprovalService(
        SmartFleetDbContext dbContext,
        IDispatchSyncService dispatchSyncService,
        ILogger<ApprovalService> logger)
    {
        _dbContext = dbContext;
        _dispatchSyncService = dispatchSyncService;
        _logger = logger;
    }

    public async Task<PagedApprovalResultDto> GetPagedApprovalsAsync(
        string? status,
        string? sortBy,
        string? sortOrder,
        string? search,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        // Auto-seed demo records if DB is fresh so supervisor dashboard is immediately active
        await SeedDemoDataIfEmptyAsync(cancellationToken);

        var query = _dbContext.ApprovalRequests
            .Include(a => a.ReviewedBy)
            .AsNoTracking()
            .AsQueryable();

        // 1. Status Filter
        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            if (Enum.TryParse<ApprovalStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(a => a.Status == parsedStatus);
            }
        }

        // 2. Text Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            query = query.Where(a =>
                a.DispatchRequestId.ToLower().Contains(s) ||
                a.RoverId.ToLower().Contains(s) ||
                a.RiskReason.ToLower().Contains(s));
        }

        // 3. Sorting
        var isAscending = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase);
        query = (sortBy?.ToLowerInvariant()) switch
        {
            "riskscore" => isAscending ? query.OrderBy(a => a.RiskScore) : query.OrderByDescending(a => a.RiskScore),
            "dispatchrequestid" => isAscending ? query.OrderBy(a => a.DispatchRequestId) : query.OrderByDescending(a => a.DispatchRequestId),
            "roverid" => isAscending ? query.OrderBy(a => a.RoverId) : query.OrderByDescending(a => a.RoverId),
            "status" => isAscending ? query.OrderBy(a => a.Status) : query.OrderByDescending(a => a.Status),
            _ => isAscending ? query.OrderBy(a => a.CreatedAt) : query.OrderByDescending(a => a.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var pageNumber = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 100);

        var items = await query
            .Skip((pageNumber - 1) * size)
            .Take(size)
            .Select(a => MapToDto(a))
            .ToListAsync(cancellationToken);

        return new PagedApprovalResultDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = size
        };
    }

    public async Task<ApprovalRequestDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var approval = await _dbContext.ApprovalRequests
            .Include(a => a.ReviewedBy)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        return approval == null ? null : MapToDto(approval);
    }

    public async Task<ApprovalRequestDto> ApproveAsync(Guid id, Guid? supervisorId, string? notes, CancellationToken cancellationToken = default)
    {
        var approval = await _dbContext.ApprovalRequests
            .Include(a => a.ReviewedBy)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"ApprovalRequest with ID '{id}' was not found.");

        if (approval.Status != ApprovalStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot approve request with current status '{approval.Status}'. Only 'Pending' requests can be approved.");
        }

        var validSupervisorId = await ResolveValidSupervisorIdAsync(supervisorId, cancellationToken);

        approval.Status = ApprovalStatus.Approved;
        approval.ReviewedById = validSupervisorId;
        approval.ReviewNotes = notes;
        approval.UpdatedAt = DateTime.UtcNow;

        // Propagate to linked DispatchRequest component via internal service
        await _dispatchSyncService.UpdateDispatchRequestStatusAsync(approval.DispatchRequestId, "Approved", notes, cancellationToken);

        // Record supervisor action into WorkflowExecutionLog
        _dbContext.WorkflowExecutionLogs.Add(new WorkflowExecutionLog
        {
            DispatchRequestId = approval.DispatchRequestId,
            StepName = "SupervisorApproval",
            AgentName = "Supervisor",
            InputJson = JsonSerializer.Serialize(new { Decision = "Approve", Notes = notes }),
            OutputJson = JsonSerializer.Serialize(new { Status = "Approved", SupervisorId = validSupervisorId, Timestamp = approval.UpdatedAt }),
            ValidationResult = "Approved",
            Timestamp = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("ApprovalRequest {Id} was approved by supervisor {SupervisorId}", id, validSupervisorId);

        return MapToDto(approval);
    }

    public async Task<ApprovalRequestDto> RejectAsync(Guid id, Guid? supervisorId, string? notes, CancellationToken cancellationToken = default)
    {
        var approval = await _dbContext.ApprovalRequests
            .Include(a => a.ReviewedBy)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"ApprovalRequest with ID '{id}' was not found.");

        if (approval.Status != ApprovalStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot reject request with current status '{approval.Status}'. Only 'Pending' requests can be rejected.");
        }

        var validSupervisorId = await ResolveValidSupervisorIdAsync(supervisorId, cancellationToken);

        approval.Status = ApprovalStatus.Rejected;
        approval.ReviewedById = validSupervisorId;
        approval.ReviewNotes = notes;
        approval.UpdatedAt = DateTime.UtcNow;

        await _dispatchSyncService.UpdateDispatchRequestStatusAsync(approval.DispatchRequestId, "Rejected", notes, cancellationToken);

        _dbContext.WorkflowExecutionLogs.Add(new WorkflowExecutionLog
        {
            DispatchRequestId = approval.DispatchRequestId,
            StepName = "SupervisorRejection",
            AgentName = "Supervisor",
            InputJson = JsonSerializer.Serialize(new { Decision = "Reject", Notes = notes }),
            OutputJson = JsonSerializer.Serialize(new { Status = "Rejected", SupervisorId = validSupervisorId, Timestamp = approval.UpdatedAt }),
            ValidationResult = "Rejected",
            Timestamp = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("ApprovalRequest {Id} was rejected by supervisor {SupervisorId}", id, validSupervisorId);

        return MapToDto(approval);
    }

    public async Task<ApprovalRequestDto> RequestRevisionAsync(Guid id, Guid? supervisorId, string? notes, CancellationToken cancellationToken = default)
    {
        var approval = await _dbContext.ApprovalRequests
            .Include(a => a.ReviewedBy)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"ApprovalRequest with ID '{id}' was not found.");

        if (approval.Status != ApprovalStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot request revision on request with status '{approval.Status}'. Only 'Pending' requests can be revised.");
        }

        var validSupervisorId = await ResolveValidSupervisorIdAsync(supervisorId, cancellationToken);

        approval.Status = ApprovalStatus.RevisionRequested;
        approval.ReviewedById = validSupervisorId;
        approval.ReviewNotes = notes;
        approval.UpdatedAt = DateTime.UtcNow;

        await _dispatchSyncService.UpdateDispatchRequestStatusAsync(approval.DispatchRequestId, "RevisionRequested", notes, cancellationToken);

        _dbContext.WorkflowExecutionLogs.Add(new WorkflowExecutionLog
        {
            DispatchRequestId = approval.DispatchRequestId,
            StepName = "SupervisorRevisionRequest",
            AgentName = "Supervisor",
            InputJson = JsonSerializer.Serialize(new { Decision = "RequestRevision", Notes = notes }),
            OutputJson = JsonSerializer.Serialize(new { Status = "RevisionRequested", SupervisorId = validSupervisorId, Timestamp = approval.UpdatedAt }),
            ValidationResult = "RevisionRequested",
            Timestamp = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("ApprovalRequest {Id} had revision requested by supervisor {SupervisorId}", id, validSupervisorId);

        return MapToDto(approval);
    }

    private async Task<Guid?> ResolveValidSupervisorIdAsync(Guid? supervisorId, CancellationToken cancellationToken)
    {
        if (supervisorId.HasValue && supervisorId.Value != Guid.Empty)
        {
            var userExists = await _dbContext.Users.AnyAsync(u => u.Id == supervisorId.Value, cancellationToken);
            if (userExists)
            {
                return supervisorId.Value;
            }
        }

        // Fallback: Check if any supervisor user exists in the database
        var supervisor = await _dbContext.Users.FirstOrDefaultAsync(u => u.Role == Role.Supervisor, cancellationToken);
        return supervisor?.Id;
    }

    public async Task<List<WorkflowExecutionLogDto>> GetExecutionLogsAsync(Guid approvalRequestId, CancellationToken cancellationToken = default)
    {
        var approval = await _dbContext.ApprovalRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == approvalRequestId, cancellationToken)
            ?? throw new KeyNotFoundException($"ApprovalRequest with ID '{approvalRequestId}' was not found.");

        var logs = await _dbContext.WorkflowExecutionLogs
            .AsNoTracking()
            .Where(w => w.DispatchRequestId == approval.DispatchRequestId)
            .OrderBy(w => w.Timestamp)
            .Select(w => new WorkflowExecutionLogDto
            {
                Id = w.Id,
                DispatchRequestId = w.DispatchRequestId,
                StepName = w.StepName,
                AgentName = w.AgentName,
                InputJson = w.InputJson,
                OutputJson = w.OutputJson,
                ValidationResult = w.ValidationResult,
                Timestamp = w.Timestamp
            })
            .ToListAsync(cancellationToken);

        return logs;
    }

    public async Task<ApprovalStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _dbContext.ApprovalRequests.CountAsync(a => a.Status == ApprovalStatus.Pending, cancellationToken);
        var approved = await _dbContext.ApprovalRequests.CountAsync(a => a.Status == ApprovalStatus.Approved, cancellationToken);
        var rejected = await _dbContext.ApprovalRequests.CountAsync(a => a.Status == ApprovalStatus.Rejected, cancellationToken);
        var revision = await _dbContext.ApprovalRequests.CountAsync(a => a.Status == ApprovalStatus.RevisionRequested, cancellationToken);

        var avgScore = await _dbContext.ApprovalRequests.AnyAsync(cancellationToken)
            ? await _dbContext.ApprovalRequests.AverageAsync(a => a.RiskScore, cancellationToken)
            : 0.0;

        return new ApprovalStatsDto
        {
            TotalPending = pending,
            TotalApproved = approved,
            TotalRejected = rejected,
            TotalRevisionRequested = revision,
            AverageRiskScore = Math.Round(avgScore, 1)
        };
    }

    public async Task SeedDemoDataIfEmptyAsync(CancellationToken cancellationToken = default)
    {
        if (await _dbContext.ApprovalRequests.AnyAsync(cancellationToken))
        {
            return;
        }

        var supervisor = await _dbContext.Users.FirstOrDefaultAsync(u => u.Role == Role.Supervisor, cancellationToken);

        // Demo item 1: Pending with moderate weather and proximity sensor note
        var d1 = "d3f1-892a";
        var r1 = "RO-04";
        var input1 = new SafetyGuardInput
        {
            DispatchRequestId = d1,
            RoverId = r1,
            MissionPlanSummary = new MissionPlanSummary
            {
                Plan = new List<PlanStepSummary>
                {
                    new() { StepNumber = 1, StepName = "Locate available rover", Status = "Completed" },
                    new() { StepNumber = 2, StepName = "Verify battery sufficient", Status = "Pending" }
                }
            },
            TelemetryResult = new TelemetryResultSummary
            {
                BatteryOk = true,
                WeatherRisk = "medium",
                Locked = true
            },
            MaintenanceResult = new MaintenanceResultSummary
            {
                BreakdownReportId = "b9a1-1209",
                LikelyPart = "Proximity Sensor Array",
                EstimatedRepairHours = 1.5,
                Severity = "Medium",
                ConfidenceNote = "Telemetry drift detected in dock area",
                RecommendedAction = "ScheduleRepair"
            }
        };

        var output1 = new SafetyGuardOutput
        {
            DispatchRequestId = d1,
            RiskScore = 65,
            RiskReason = "Moderate weather risk across transit zone, Subsystem alert logged (b9a1-1209: Proximity Sensor Array)",
            RequiresApproval = true,
            AutoOutcome = "PendingApproval"
        };

        var req1 = new ApprovalRequest
        {
            Id = Guid.NewGuid(),
            DispatchRequestId = d1,
            RoverId = r1,
            AgentSummaryJson = JsonSerializer.Serialize(input1),
            RiskScore = 65,
            RiskReason = output1.RiskReason,
            Status = ApprovalStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddMinutes(-45),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-45)
        };

        // Demo item 2: Pending with low battery margin
        var d2 = "d7e2-1104";
        var r2 = "RO-02";
        var input2 = new SafetyGuardInput
        {
            DispatchRequestId = d2,
            RoverId = r2,
            MissionPlanSummary = new MissionPlanSummary
            {
                Plan = new List<PlanStepSummary>
                {
                    new() { StepNumber = 1, StepName = "Assign freight corridor B", Status = "Completed" },
                    new() { StepNumber = 2, StepName = "Navigate ramp incline", Status = "Pending" }
                }
            },
            TelemetryResult = new TelemetryResultSummary
            {
                BatteryOk = false,
                WeatherRisk = "low",
                Locked = true
            },
            MaintenanceResult = null
        };

        var output2 = new SafetyGuardOutput
        {
            DispatchRequestId = d2,
            RiskScore = 45,
            RiskReason = "Insufficient battery reserve for mission profile",
            RequiresApproval = true,
            AutoOutcome = "PendingApproval"
        };

        var req2 = new ApprovalRequest
        {
            Id = Guid.NewGuid(),
            DispatchRequestId = d2,
            RoverId = r2,
            AgentSummaryJson = JsonSerializer.Serialize(input2),
            RiskScore = 45,
            RiskReason = output2.RiskReason,
            Status = ApprovalStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddMinutes(-20),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-20)
        };

        // Demo item 3: Approved request
        var d3 = "d4a9-5521";
        var r3 = "RO-07";
        var input3 = new SafetyGuardInput
        {
            DispatchRequestId = d3,
            RoverId = r3,
            MissionPlanSummary = new MissionPlanSummary
            {
                Plan = new List<PlanStepSummary>
                {
                    new() { StepNumber = 1, StepName = "Confirm dock alignment", Status = "Completed" }
                }
            },
            TelemetryResult = new TelemetryResultSummary
            {
                BatteryOk = true,
                WeatherRisk = "medium",
                Locked = true
            },
            MaintenanceResult = null
        };

        var req3 = new ApprovalRequest
        {
            Id = Guid.NewGuid(),
            DispatchRequestId = d3,
            RoverId = r3,
            AgentSummaryJson = JsonSerializer.Serialize(input3),
            RiskScore = 40,
            RiskReason = "Moderate weather risk across transit zone",
            Status = ApprovalStatus.Approved,
            ReviewedById = supervisor?.Id,
            ReviewNotes = "Supervisor confirmed indoor weather mitigation active. Proceeding with caution.",
            CreatedAt = DateTime.UtcNow.AddHours(-3),
            UpdatedAt = DateTime.UtcNow.AddHours(-2)
        };

        _dbContext.ApprovalRequests.AddRange(req1, req2, req3);

        // Execution logs for d1
        _dbContext.WorkflowExecutionLogs.AddRange(
            new WorkflowExecutionLog
            {
                DispatchRequestId = d1,
                StepName = "MissionPlanning",
                AgentName = "MissionPlannerAgent",
                InputJson = JsonSerializer.Serialize(new { sourceZone = "WarehouseA-DockA1", destinationZone = "WarehouseA-DockB3", cargoType = "Fragile", priority = "High" }),
                OutputJson = JsonSerializer.Serialize(input1.MissionPlanSummary),
                ValidationResult = "PlanGenerated",
                Timestamp = DateTime.UtcNow.AddMinutes(-48)
            },
            new WorkflowExecutionLog
            {
                DispatchRequestId = d1,
                StepName = "TelemetryVerification",
                AgentName = "DispatchTelemetryAgent",
                InputJson = JsonSerializer.Serialize(new { selectedRoverId = r1 }),
                OutputJson = JsonSerializer.Serialize(input1.TelemetryResult),
                ValidationResult = "TelemetryAggregated",
                Timestamp = DateTime.UtcNow.AddMinutes(-46)
            },
            new WorkflowExecutionLog
            {
                DispatchRequestId = d1,
                StepName = "SafetyGuardEvaluation",
                AgentName = "SafetyGuardAgent",
                InputJson = JsonSerializer.Serialize(input1),
                OutputJson = JsonSerializer.Serialize(output1),
                ValidationResult = output1.AutoOutcome,
                Timestamp = DateTime.UtcNow.AddMinutes(-45)
            }
        );

        // Execution logs for d3 (including supervisor sign-off)
        _dbContext.WorkflowExecutionLogs.AddRange(
            new WorkflowExecutionLog
            {
                DispatchRequestId = d3,
                StepName = "SafetyGuardEvaluation",
                AgentName = "SafetyGuardAgent",
                InputJson = JsonSerializer.Serialize(input3),
                OutputJson = JsonSerializer.Serialize(new { dispatchRequestId = d3, riskScore = 40, autoOutcome = "PendingApproval" }),
                ValidationResult = "PendingApproval",
                Timestamp = DateTime.UtcNow.AddHours(-3)
            },
            new WorkflowExecutionLog
            {
                DispatchRequestId = d3,
                StepName = "SupervisorApproval",
                AgentName = "Supervisor",
                InputJson = JsonSerializer.Serialize(new { Decision = "Approve", Notes = req3.ReviewNotes }),
                OutputJson = JsonSerializer.Serialize(new { Status = "Approved", SupervisorId = supervisor?.Id }),
                ValidationResult = "Approved",
                Timestamp = DateTime.UtcNow.AddHours(-2)
            }
        );

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ApprovalRequestDto MapToDto(ApprovalRequest entity)
    {
        return new ApprovalRequestDto
        {
            Id = entity.Id,
            DispatchRequestId = entity.DispatchRequestId,
            RoverId = entity.RoverId,
            AgentSummaryJson = entity.AgentSummaryJson,
            RiskScore = entity.RiskScore,
            RiskReason = entity.RiskReason,
            Status = entity.Status,
            ReviewedById = entity.ReviewedById,
            ReviewedByName = entity.ReviewedBy?.Name,
            ReviewNotes = entity.ReviewNotes,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
