using System.ComponentModel.DataAnnotations;
using SmartFleet.Backend.Models.Enums;

namespace SmartFleet.Backend.DTOs.Rovers;

public class RoverDto
{
    public Guid Id { get; set; }
    public string Identifier { get; set; } = string.Empty;
    public RoverStatus Status { get; set; }
    public int BatteryPercentage { get; set; }
    public string LocationZone { get; set; } = string.Empty;
    public string? CurrentMissionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class RoverQueryParameters
{
    public RoverStatus? Status { get; set; }
    public string? Zone { get; set; }
    public string? SortBy { get; set; } = "identifier"; // "identifier", "battery", "status", "zone"
    public bool IsDescending { get; set; } = false;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize));
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

public class UpdateRoverSimulationDto
{
    public RoverStatus? Status { get; set; }

    [Range(0, 100, ErrorMessage = "Battery percentage must be between 0 and 100.")]
    public int? BatteryPercentage { get; set; }

    [MaxLength(64)]
    public string? LocationZone { get; set; }
}

public class LockRoverRequestDto
{
    [Required(ErrorMessage = "MissionId is required to lock a rover.")]
    public string MissionId { get; set; } = string.Empty;
}

public class LockRoverResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public RoverDto? Rover { get; set; }
}
