using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartFleet.Backend.Agents.MissionPlannerAgent;
using SmartFleet.Backend.DTOs.Common;
using SmartFleet.Backend.DTOs.Dispatch;
using SmartFleet.Backend.Services.Interfaces;

namespace SmartFleet.Backend.Controllers;

[ApiController]
[Route("api/dispatch-requests")]
[Authorize]
public class DispatchRequestsController : ControllerBase
{
    private readonly IDispatchService _dispatchService;
    private readonly ILogger<DispatchRequestsController> _logger;

    public DispatchRequestsController(
        IDispatchService dispatchService,
        ILogger<DispatchRequestsController> logger)
    {
        _dispatchService = dispatchService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new cargo dispatch request order (Operator or Supervisor).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Operator,Supervisor")]
    [ProducesResponseType(typeof(DispatchRequestResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateDispatchRequestDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        var result = await _dispatchService.CreateDispatchRequestAsync(dto, userId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Retrieves paginated dispatch requests with search, filter, and sort options.
    /// Operators see only their requests; Supervisors and Technicians can view all.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<DispatchRequestResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromQuery] DispatchRequestQueryDto query, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        var result = await _dispatchService.GetDispatchRequestsAsync(query, userId, role, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a single dispatch request by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DispatchRequestResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        var result = await _dispatchService.GetDispatchRequestByIdAsync(id, userId, role, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Updates the status of an existing dispatch request.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(DispatchRequestResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateDispatchRequestStatusDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        var result = await _dispatchService.UpdateStatusAsync(id, dto.Status, userId, role, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Triggers the Mission Planner Agent to generate a multi-step mission checklist and persist workflow state.
    /// </summary>
    [HttpPost("{id:guid}/plan")]
    [ProducesResponseType(typeof(MissionPlannerOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GeneratePlan([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        var result = await _dispatchService.GeneratePlanAsync(id, userId, role, cancellationToken);
        return Ok(result);
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User identifier claim not found.");
        return Guid.Parse(claim);
    }

    private string GetCurrentUserRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? "Operator";
    }
}
