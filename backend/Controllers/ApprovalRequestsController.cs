using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartFleet.Backend.Agents.SafetyGuardAgent;
using SmartFleet.Backend.Agents.SafetyGuardAgent.DTOs;
using SmartFleet.Backend.DTOs.Approvals;
using SmartFleet.Backend.DTOs.Common;
using SmartFleet.Backend.Services.Interfaces;

namespace SmartFleet.Backend.Controllers;

[ApiController]
[Route("api/approval-requests")]
[Authorize]
public class ApprovalRequestsController : ControllerBase
{
    private readonly IApprovalService _approvalService;
    private readonly ISafetyGuardAgent _safetyGuardAgent;
    private readonly ILogger<ApprovalRequestsController> _logger;

    public ApprovalRequestsController(
        IApprovalService approvalService,
        ISafetyGuardAgent safetyGuardAgent,
        ILogger<ApprovalRequestsController> logger)
    {
        _approvalService = approvalService;
        _safetyGuardAgent = safetyGuardAgent;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paginated list of approval requests with optional status filtering, sorting, and search.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedApprovalResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApprovalRequests(
        [FromQuery] string? status,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] string? sortOrder = "desc",
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _approvalService.GetPagedApprovalsAsync(
            status, sortBy, sortOrder, search, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets aggregated metrics for the approval center dashboard.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApprovalStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken = default)
    {
        var stats = await _approvalService.GetStatsAsync(cancellationToken);
        return Ok(stats);
    }

    /// <summary>
    /// Retrieves a single approval request by its unique identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApprovalRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApprovalRequestById(Guid id, CancellationToken cancellationToken = default)
    {
        var request = await _approvalService.GetByIdAsync(id, cancellationToken);
        if (request == null)
        {
            return NotFound(new ErrorResponse { StatusCode = StatusCodes.Status404NotFound, Message = "Approval request was not found." });
        }
        return Ok(request);
    }

    /// <summary>
    /// Supervisor action: Approves a pending dispatch approval request.
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Supervisor")]
    [ProducesResponseType(typeof(ApprovalRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(
        Guid id,
        [FromBody] ApprovalDecisionDto? dto,
        CancellationToken cancellationToken = default)
    {
        var supervisorId = GetCurrentUserId();
        try
        {
            var updated = await _approvalService.ApproveAsync(id, supervisorId, dto?.ReviewNotes, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse { StatusCode = StatusCodes.Status404NotFound, Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponse { StatusCode = StatusCodes.Status400BadRequest, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to approve approval request {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = ex.InnerException?.Message ?? ex.Message
            });
        }
    }

    /// <summary>
    /// Supervisor action: Rejects a pending dispatch approval request.
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Supervisor")]
    [ProducesResponseType(typeof(ApprovalRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] ApprovalDecisionDto? dto,
        CancellationToken cancellationToken = default)
    {
        var supervisorId = GetCurrentUserId();
        try
        {
            var updated = await _approvalService.RejectAsync(id, supervisorId, dto?.ReviewNotes, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse { StatusCode = StatusCodes.Status404NotFound, Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponse { StatusCode = StatusCodes.Status400BadRequest, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reject approval request {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = ex.InnerException?.Message ?? ex.Message
            });
        }
    }

    /// <summary>
    /// Supervisor action: Requests revision on a pending dispatch approval request.
    /// </summary>
    [HttpPost("{id:guid}/request-revision")]
    [Authorize(Roles = "Supervisor")]
    [ProducesResponseType(typeof(ApprovalRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RequestRevision(
        Guid id,
        [FromBody] ApprovalDecisionDto? dto,
        CancellationToken cancellationToken = default)
    {
        var supervisorId = GetCurrentUserId();
        try
        {
            var updated = await _approvalService.RequestRevisionAsync(id, supervisorId, dto?.ReviewNotes, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse { StatusCode = StatusCodes.Status404NotFound, Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponse { StatusCode = StatusCodes.Status400BadRequest, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request revision for approval request {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = ex.InnerException?.Message ?? ex.Message
            });
        }
    }

    /// <summary>
    /// Fetches the auditable execution summary / workflow logs for an approval request.
    /// </summary>
    [HttpGet("{id:guid}/execution-log")]
    [ProducesResponseType(typeof(List<WorkflowExecutionLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExecutionLog(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var logs = await _approvalService.GetExecutionLogsAsync(id, cancellationToken);
            return Ok(logs);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse { StatusCode = StatusCodes.Status404NotFound, Message = ex.Message });
        }
    }

    /// <summary>
    /// Executes Safety Guard Agent evaluation on a provided input or simulated scenario.
    /// Used for testing and live pipeline demonstrations in the Approval Center.
    /// </summary>
    [HttpPost("evaluate")]
    [ProducesResponseType(typeof(SafetyGuardOutput), StatusCodes.Status200OK)]
    public async Task<IActionResult> Evaluate(
        [FromBody] SimulateEvaluationDto? dto,
        CancellationToken cancellationToken = default)
    {
        var scenario = dto?.Scenario ?? "pending-approval";
        var input = _safetyGuardAgent.CreateMockedInput(scenario, dto?.DispatchRequestId, dto?.RoverId);
        var output = await _safetyGuardAgent.EvaluateSafetyAsync(input, cancellationToken);
        return Ok(output);
    }

    private Guid? GetCurrentUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirst("nameid")?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.Sid)?.Value;

        return (!string.IsNullOrEmpty(claimValue) && Guid.TryParse(claimValue, out var guid) && guid != Guid.Empty)
            ? guid
            : null;
    }
}
