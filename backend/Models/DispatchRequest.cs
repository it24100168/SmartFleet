using SmartFleet.Backend.Models.Enums;

namespace SmartFleet.Backend.Models;

/// <summary>
/// Represents a cargo transport dispatch request submitted by a Factory Operator.
/// </summary>
public class DispatchRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key identifying the Operator who created this dispatch request.
    /// </summary>
    public Guid OperatorId { get; set; }

    /// <summary>
    /// Navigation property for the requesting Operator.
    /// </summary>
    public User? Operator { get; set; }

    /// <summary>
    /// Foreign key referencing the assigned Rover (nullable; to be linked once Rover module is integrated).
    /// </summary>
    public Guid? RoverId { get; set; }

    /// <summary>
    /// Source pickup zone (e.g. WarehouseA-DockA1).
    /// </summary>
    public string SourceZone { get; set; } = string.Empty;

    /// <summary>
    /// Target dropoff zone (e.g. WarehouseA-DockB3).
    /// </summary>
    public string DestinationZone { get; set; } = string.Empty;

    /// <summary>
    /// Classification of cargo (e.g. Fragile, Standard, Hazmat, Refrigerated).
    /// </summary>
    public string CargoType { get; set; } = string.Empty;

    /// <summary>
    /// Operational priority (e.g. Low, Medium, High, Critical).
    /// </summary>
    public string Priority { get; set; } = "Medium";

    /// <summary>
    /// Requested completion or pickup time window in UTC.
    /// </summary>
    public DateTime PreferredTimeWindow { get; set; }

    /// <summary>
    /// Current lifecycle status of the dispatch order.
    /// </summary>
    public DispatchRequestStatus Status { get; set; } = DispatchRequestStatus.Pending;

    /// <summary>
    /// Optional GPS latitude captured from the mobile device upon submission.
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// Optional GPS longitude captured from the mobile device upon submission.
    /// </summary>
    public double? Longitude { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Associated workflow runs and planning state executions.
    /// </summary>
    public ICollection<WorkflowRun> WorkflowRuns { get; set; } = new List<WorkflowRun>();
}
