namespace SmartFleet.Backend.Models;

/// <summary>
/// Lookup table representing diagnostic failure patterns used by the Maintenance Mechanic Agent.
/// </summary>
public class FailureCatalog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Keyword or key phrase searched in breakdown descriptions/symptoms (e.g., "motor overheating").
    /// </summary>
    public string SymptomKeyword { get; set; } = string.Empty;

    /// <summary>
    /// Symptom category (e.g., "MotorOverheating", "WheelJam", "SensorFault").
    /// </summary>
    public string SymptomCategory { get; set; } = string.Empty;

    /// <summary>
    /// The suspected faulty part or assembly recommended for replacement or repair.
    /// </summary>
    public string LikelyPart { get; set; } = string.Empty;

    /// <summary>
    /// Estimated time in hours to perform repair diagnostics and replacement.
    /// </summary>
    public int EstimatedRepairHours { get; set; }

    /// <summary>
    /// Defect severity level: Low, Medium, High, or Critical.
    /// </summary>
    public string Severity { get; set; } = "Medium";
}
