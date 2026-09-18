using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartFleet.Backend.Data.Repositories;
using SmartFleet.Backend.DTOs.Rovers;
using SmartFleet.Backend.Models;

namespace SmartFleet.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoversController : ControllerBase
{
    private readonly IRoverRepository _roverRepository;
    private readonly ILogger<RoversController> _logger;

    public RoversController(IRoverRepository roverRepository, ILogger<RoversController> logger)
    {
        _roverRepository = roverRepository;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paginated list of rovers with optional status and zone filtering.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<RoverDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRovers([FromQuery] RoverQueryParameters parameters, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _roverRepository.GetPagedAsync(
            parameters.Status,
            parameters.Zone,
            parameters.SortBy,
            parameters.IsDescending,
            parameters.Page,
            parameters.PageSize,
            cancellationToken);

        var response = new PagedResult<RoverDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = parameters.Page,
            PageSize = parameters.PageSize
        };

        return Ok(response);
    }

    /// <summary>
    /// Retrieves details for a specific rover by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RoverDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoverById(Guid id, CancellationToken cancellationToken)
    {
        var rover = await _roverRepository.GetByIdAsync(id, cancellationToken);
        if (rover == null)
        {
            return NotFound(new { message = $"Rover with ID '{id}' was not found." });
        }

        return Ok(MapToDto(rover));
    }

    /// <summary>
    /// Manually updates status, battery percentage, or zone for simulation and testing purposes.
    /// </summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Technician,Supervisor,Operator")]
    [ProducesResponseType(typeof(RoverDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRoverSimulation(
        Guid id,
        [FromBody] UpdateRoverSimulationDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var rover = await _roverRepository.GetByIdAsync(id, cancellationToken);
        if (rover == null)
        {
            return NotFound(new { message = $"Rover with ID '{id}' was not found." });
        }

        if (dto.Status.HasValue)
        {
            rover.Status = dto.Status.Value;
            if (dto.Status.Value == Models.Enums.RoverStatus.Idle)
            {
                rover.CurrentMissionId = null;
            }
        }

        if (dto.BatteryPercentage.HasValue)
        {
            rover.BatteryPercentage = dto.BatteryPercentage.Value;
        }

        if (!string.IsNullOrWhiteSpace(dto.LocationZone))
        {
            rover.LocationZone = dto.LocationZone.Trim();
        }

        await _roverRepository.UpdateAsync(rover, cancellationToken);
        _logger.LogInformation("Rover {RoverId} ({Identifier}) simulation state updated by {User}.", rover.Id, rover.Identifier, User.Identity?.Name);

        return Ok(MapToDto(rover));
    }

    /// <summary>
    /// Transactionally locks a rover for a mission, preventing double-booking using isolated DB transactions.
    /// </summary>
    [HttpPost("{id:guid}/lock")]
    [Authorize(Roles = "Operator,Supervisor")]
    [ProducesResponseType(typeof(LockRoverResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LockRoverForMission(
        Guid id,
        [FromBody] LockRoverRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var lockedRover = await _roverRepository.LockRoverForMissionAsync(id, request.MissionId, cancellationToken);
            if (lockedRover == null)
            {
                return NotFound(new LockRoverResponseDto
                {
                    Success = false,
                    Message = $"Rover with ID '{id}' was not found."
                });
            }

            _logger.LogInformation("Rover {RoverId} ({Identifier}) locked successfully for mission {MissionId}.",
                lockedRover.Id, lockedRover.Identifier, request.MissionId);

            return Ok(new LockRoverResponseDto
            {
                Success = true,
                Message = $"Rover '{lockedRover.Identifier}' successfully locked for mission '{request.MissionId}'.",
                Rover = MapToDto(lockedRover)
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to lock rover {RoverId} for mission {MissionId}: {Message}", id, request.MissionId, ex.Message);
            return BadRequest(new LockRoverResponseDto
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    private static RoverDto MapToDto(Rover rover) => new()
    {
        Id = rover.Id,
        Identifier = rover.Identifier,
        Status = rover.Status,
        BatteryPercentage = rover.BatteryPercentage,
        LocationZone = rover.LocationZone,
        CurrentMissionId = rover.CurrentMissionId,
        CreatedAt = rover.CreatedAt,
        UpdatedAt = rover.UpdatedAt
    };
}
