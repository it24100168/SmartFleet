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
    private readonly WorkflowOrchestrator _workflow;
    private readonly ILogger<ApprovalService> _logger;

    public ApprovalService(
        SmartFleetDbContext dbContext,
        WorkflowOrchestrator workflow,
        ILogger<ApprovalService> logger)
    {
        _dbContext = dbContext;
        _workflow = workflow;
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
        => MapToDto(await _workflow.DecideAsync(id, supervisorId, "Approved", notes, cancellationToken));
    public async Task<ApprovalRequestDto> RejectAsync(Guid id, Guid? supervisorId, string? notes, CancellationToken cancellationToken = default)
        => MapToDto(await _workflow.DecideAsync(id, supervisorId, "Rejected", notes, cancellationToken));
    public async Task<ApprovalRequestDto> RequestRevisionAsync(Guid id, Guid? supervisorId, string? notes, CancellationToken cancellationToken = default)
        => MapToDto(await _workflow.DecideAsync(id, supervisorId, "RevisionRequested", notes, cancellationToken));
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

    public Task SeedDemoDataIfEmptyAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
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
