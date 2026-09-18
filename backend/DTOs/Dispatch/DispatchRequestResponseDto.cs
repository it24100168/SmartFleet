using SmartFleet.Backend.Agents.MissionPlannerAgent;
using SmartFleet.Backend.Models.Enums;

namespace SmartFleet.Backend.DTOs.Dispatch;

/// <summary>
/// Detailed response model for a dispatch request.
/// </summary>
public class DispatchRequestResponseDto
{
    public Guid Id { get; set; }
    public Guid OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public string OperatorEmail { get; set; } = string.Empty;
    public Guid? RoverId { get; set; }
    public string SourceZone { get; set; } = string.Empty;
    public string DestinationZone { get; set; } = string.Empty;
    public string CargoType { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime PreferredTimeWindow { get; set; }
    public DispatchRequestStatus Status { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Latest generated mission plan checklist if available.
    /// </summary>
    public MissionPlannerOutput? LatestPlan { get; set; }
}
