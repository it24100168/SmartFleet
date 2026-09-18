using System.Text.Json;
using SmartFleet.Backend.Agents.MissionPlannerAgent;
using SmartFleet.Backend.Data.Repositories;
using SmartFleet.Backend.DTOs.Common;
using SmartFleet.Backend.DTOs.Dispatch;
using SmartFleet.Backend.Models;
using SmartFleet.Backend.Models.Enums;
using SmartFleet.Backend.Services.Interfaces;

namespace SmartFleet.Backend.Services;

public class DispatchService : IDispatchService
{
    private readonly IDispatchRequestRepository _dispatchRepository;
    private readonly IWorkflowRunRepository _workflowRunRepository;
    private readonly IMissionPlannerAgent _missionPlannerAgent;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<DispatchService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public DispatchService(
        IDispatchRequestRepository dispatchRepository,
        IWorkflowRunRepository workflowRunRepository,
        IMissionPlannerAgent missionPlannerAgent,
        IUserRepository userRepository,
        ILogger<DispatchService> logger)
    {
        _dispatchRepository = dispatchRepository;
        _workflowRunRepository = workflowRunRepository;
        _missionPlannerAgent = missionPlannerAgent;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<DispatchRequestResponseDto> CreateDispatchRequestAsync(
        CreateDispatchRequestDto dto,
        Guid operatorId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(operatorId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Your operator account was not found or your session is stale. Please log in again.");

        var request = new DispatchRequest
        {
            Id = Guid.NewGuid(),
            OperatorId = operatorId,
            SourceZone = dto.SourceZone.Trim(),
            DestinationZone = dto.DestinationZone.Trim(),
            CargoType = dto.CargoType.Trim(),
            Priority = string.IsNullOrWhiteSpace(dto.Priority) ? "Medium" : dto.Priority.Trim(),
            PreferredTimeWindow = dto.PreferredTimeWindow.ToUniversalTime(),
            Status = DispatchRequestStatus.Pending,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _dispatchRepository.AddAsync(request, cancellationToken);
        await _dispatchRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created DispatchRequest {Id} by Operator {OperatorId}", request.Id, operatorId);

        request.Operator = user;
        return MapToDto(request);
    }

    public async Task<PagedResultDto<DispatchRequestResponseDto>> GetDispatchRequestsAsync(
        DispatchRequestQueryDto query,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken cancellationToken = default)
    {
        // Role check: Operator can only view own requests; Supervisor / Technician can view all
        Guid? operatorIdFilter = currentUserRole.Equals(Role.Operator.ToString(), StringComparison.OrdinalIgnoreCase)
            ? currentUserId
            : null;

        var pagedEntities = await _dispatchRepository.GetPagedAsync(query, operatorIdFilter, cancellationToken);

        var dtoList = pagedEntities.Items.Select(MapToDto).ToList();

        return new PagedResultDto<DispatchRequestResponseDto>
        {
            Items = dtoList,
            TotalCount = pagedEntities.TotalCount,
            Page = pagedEntities.Page,
            PageSize = pagedEntities.PageSize
        };
    }

    public async Task<DispatchRequestResponseDto> GetDispatchRequestByIdAsync(
        Guid id,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken cancellationToken = default)
    {
        var request = await _dispatchRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"DispatchRequest with ID {id} was not found.");

        // Operator access restriction
        if (currentUserRole.Equals(Role.Operator.ToString(), StringComparison.OrdinalIgnoreCase) && request.OperatorId != currentUserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to view this dispatch request.");
        }

        return MapToDto(request);
    }

    public async Task<DispatchRequestResponseDto> UpdateStatusAsync(
        Guid id,
        DispatchRequestStatus status,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken cancellationToken = default)
    {
        var request = await _dispatchRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"DispatchRequest with ID {id} was not found.");

        if (currentUserRole.Equals(Role.Operator.ToString(), StringComparison.OrdinalIgnoreCase) && request.OperatorId != currentUserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to modify this dispatch request.");
        }

        request.Status = status;
        request.UpdatedAt = DateTime.UtcNow;

        await _dispatchRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Updated status for DispatchRequest {Id} to {Status}", id, status);

        return MapToDto(request);
    }

    public async Task<MissionPlannerOutput> GeneratePlanAsync(
        Guid id,
        Guid currentUserId,
        string currentUserRole,
        CancellationToken cancellationToken = default)
    {
        var request = await _dispatchRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"DispatchRequest with ID {id} was not found.");

        if (currentUserRole.Equals(Role.Operator.ToString(), StringComparison.OrdinalIgnoreCase) && request.OperatorId != currentUserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to plan this dispatch request.");
        }

        // Build contract input matching docs/agent-contracts.md strictly
        var agentInput = new MissionPlannerInput
        {
            DispatchRequestId = request.Id.ToString(),
            SourceZone = request.SourceZone,
            DestinationZone = request.DestinationZone,
            CargoType = request.CargoType,
            Priority = request.Priority,
            PreferredTimeWindow = request.PreferredTimeWindow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        // Trigger MissionPlannerAgent (plans and labels steps with assignedAgent)
        var planOutput = await _missionPlannerAgent.GeneratePlanAsync(agentInput, cancellationToken);

        // Persist WorkflowRun state
        var workflowRun = new WorkflowRun
        {
            Id = Guid.NewGuid(),
            DispatchRequestId = request.Id,
            ObjectiveJson = JsonSerializer.Serialize(agentInput, JsonOptions),
            PlanJson = JsonSerializer.Serialize(planOutput, JsonOptions),
            CurrentStep = 1,
            Status = "Generated",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _workflowRunRepository.AddAsync(workflowRun, cancellationToken);

        // Transition dispatch request status to Planned
        request.Status = DispatchRequestStatus.Planned;
        request.UpdatedAt = DateTime.UtcNow;

        await _dispatchRepository.SaveChangesAsync(cancellationToken);
        await _workflowRunRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Mission plan generated and persisted for DispatchRequest {Id}. Created WorkflowRun {WorkflowRunId}",
            id,
            workflowRun.Id);

        return planOutput;
    }

    private static DispatchRequestResponseDto MapToDto(DispatchRequest d)
    {
        MissionPlannerOutput? latestPlan = null;
        var latestWorkflow = d.WorkflowRuns.FirstOrDefault();
        if (latestWorkflow != null && !string.IsNullOrWhiteSpace(latestWorkflow.PlanJson))
        {
            try
            {
                latestPlan = JsonSerializer.Deserialize<MissionPlannerOutput>(latestWorkflow.PlanJson, JsonOptions);
            }
            catch
            {
                // Fallback gracefully if parsing fails
            }
        }

        return new DispatchRequestResponseDto
        {
            Id = d.Id,
            OperatorId = d.OperatorId,
            OperatorName = d.Operator?.Name ?? string.Empty,
            OperatorEmail = d.Operator?.Email ?? string.Empty,
            RoverId = d.RoverId,
            SourceZone = d.SourceZone,
            DestinationZone = d.DestinationZone,
            CargoType = d.CargoType,
            Priority = d.Priority,
            PreferredTimeWindow = d.PreferredTimeWindow,
            Status = d.Status,
            Latitude = d.Latitude,
            Longitude = d.Longitude,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt,
            LatestPlan = latestPlan
        };
    }
}
