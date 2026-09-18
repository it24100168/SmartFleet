namespace SmartFleet.Backend.Models.Enums;

/// <summary>
/// Defines the lifecycle status of a breakdown report incident.
/// </summary>
public enum BreakdownStatus
{
    Reported,
    Diagnosing,
    ScheduledForRepair,
    Repaired
}
