using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SmartFleet.Backend.Data.Repositories;
using SmartFleet.Backend.DTOs.Rovers;
using SmartFleet.Backend.Models;
using SmartFleet.Backend.Models.Enums;
using SmartFleet.Backend.Services;

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

    /// <summary>Registers a new, idle simulation rover in a mapped zone.</summary>
    [HttpPost]
    [Authorize(Roles = "Supervisor")]
    [ProducesResponseType(typeof(RoverDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterRover([FromBody] RegisterRoverDto dto, CancellationToken cancellationToken)
    {
        var zone = dto.LocationZone.Trim();
        if (!WarehouseLayout.IsKnown(zone))
            return BadRequest(new { message = "Choose a location from the warehouse map." });

        var identifier = dto.Identifier.Trim().ToUpperInvariant();
        if (await _roverRepository.GetByIdentifierAsync(identifier, cancellationToken) != null)
            return Conflict(new { message = "A rover with this identifier already exists." });

        var rover = new Rover
        {
            Identifier = identifier,
            LocationZone = zone,
            Status = RoverStatus.Idle,
            BatteryPercentage = 100
        };
        try
        {
            await _roverRepository.AddAsync(rover, cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Conflict(new { message = "A rover with this identifier already exists." });
        }

        _logger.LogInformation("Rover {RoverId} registered as {Identifier}", rover.Id, rover.Identifier);
        return CreatedAtAction(nameof(GetRoverById), new { id = rover.Id }, MapToDto(rover));
    }

    /// <summary>Changes the identity or mapped zone of an idle, fault-free rover.</summary>
    [HttpPut("{id:guid}/configuration")]
    [Authorize(Roles = "Supervisor")]
    [ProducesResponseType(typeof(RoverDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRoverConfiguration(Guid id, [FromBody] UpdateRoverConfigurationDto dto, CancellationToken cancellationToken)
    {
        var zone = dto.LocationZone.Trim();
        if (!WarehouseLayout.IsKnown(zone))
            return BadRequest(new { message = "Choose a location from the warehouse map." });

        if (await _roverRepository.GetByIdAsync(id, cancellationToken) == null)
            return NotFound(new { message = "Rover not found." });

        var identifier = dto.Identifier.Trim().ToUpperInvariant();
        var duplicate = await _roverRepository.GetByIdentifierAsync(identifier, cancellationToken);
        if (duplicate != null && duplicate.Id != id)
            return Conflict(new { message = "A rover with this identifier already exists." });

        Rover? rover;
        try
        {
            rover = await _roverRepository.UpdateConfigurationAsync(id, identifier, zone, cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(new { message = "A rover with this identifier already exists." });
        }
        if (rover == null)
            return Conflict(new { message = "Only an idle rover without a mission or open fault can be reconfigured." });

        _logger.LogInformation("Rover {RoverId} configuration updated", id);
        return Ok(MapToDto(rover));
    }

    /// <summary>
    /// Manually updates status, battery percentage, or zone for simulation and testing purposes.
    /// </summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Supervisor")]
    [ProducesResponseType(typeof(RoverDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRoverSimulation(
        Guid id,
        [FromBody] UpdateRoverSimulationDto dto,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return Conflict(new { message = "Use Fleet Simulation controls or maintenance repair actions; raw state overrides are disabled." });
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

        await Task.CompletedTask;
        return Conflict(new { message = "Rovers are reserved by the workflow orchestrator only." });
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
