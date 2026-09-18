using SmartFleet.Backend.Models.Enums;

namespace SmartFleet.Backend.Models;

/// <summary>
/// Represents a reported rover malfunction or breakdown incident.
/// </summary>
public class BreakdownReport
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Identifier of the affected rover (nullable column reserved for team integration).
    /// </summary>
    public Guid? RoverId { get; set; }

    /// <summary>
    /// User identifier of the Operator or Technician who filed the report.
    /// </summary>
    public Guid ReportedById { get; set; }

    /// <summary>
    /// Navigation property to the reporting user.
    /// </summary>
    public User? ReportedBy { get; set; }

    /// <summary>
    /// Symptom category (e.g. MotorOverheating, WheelJam, SensorFault, BatteryDegradation).
    /// </summary>
    public string SymptomCategory { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the observed anomaly or mechanical failure.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Relative URL to the uploaded evidence photograph (e.g., /uploads/breakdowns/xyz.jpg).
    /// </summary>
    public string? PhotoUrl { get; set; }

    /// <summary>
    /// Optional rover diagnostic error code (e.g., E204).
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Current lifecycle status: Reported, Diagnosing, ScheduledForRepair, Repaired.
    /// </summary>
    public BreakdownStatus Status { get; set; } = BreakdownStatus.Reported;

    /// <summary>
    /// Serialized JSON output from the Maintenance Mechanic Agent once diagnosed.
    /// </summary>
    public string? DiagnosisResultJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
