using SmartFleet.Backend.Models.Enums;

namespace SmartFleet.Backend.Models;

/// <summary>
/// Represents a simulated autonomous warehouse rover.
/// </summary>
public class Rover
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Unique rover code name (e.g., "RO-01", "RO-04").
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Current operating status.
    /// </summary>
    public RoverStatus Status { get; set; } = RoverStatus.Idle;

    /// <summary>
    /// Battery level percentage (0 to 100).
    /// </summary>
    public int BatteryPercentage { get; set; } = 100;

    /// <summary>
    /// Current zone or dock in the warehouse (e.g., "WarehouseA-DockA1").
    /// </summary>
    public string LocationZone { get; set; } = "WarehouseA-DockA1";

    /// <summary>
    /// Identifier of the currently assigned dispatch mission, or null if idle.
    /// </summary>
    public string? CurrentMissionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Concurrency token to prevent race conditions during mission locking.
    /// </summary>
    public uint Version { get; set; }
}
