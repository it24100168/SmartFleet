namespace SmartFleet.Backend.Models.Enums;

/// <summary>
/// Operating lifecycle status for warehouse rovers.
/// </summary>
public enum RoverStatus
{
    Idle,
    Dispatched,
    Charging,
    Maintenance,
    Faulted
}
