using System.Text.Json.Serialization;

namespace SmartFleet.Backend.Agents.MissionPlannerAgent;

/// <summary>
/// Contract input payload strictly matching docs/agent-contracts.md under "Mission Planner Agent".
/// </summary>
public class MissionPlannerInput
{
    [JsonPropertyName("dispatchRequestId")]
    public string DispatchRequestId { get; set; } = string.Empty;

    [JsonPropertyName("sourceZone")]
    public string SourceZone { get; set; } = string.Empty;

    [JsonPropertyName("destinationZone")]
    public string DestinationZone { get; set; } = string.Empty;

    [JsonPropertyName("cargoType")]
    public string CargoType { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = string.Empty;

    [JsonPropertyName("preferredTimeWindow")]
    public string PreferredTimeWindow { get; set; } = string.Empty;
}

/// <summary>
/// Contract output payload strictly matching docs/agent-contracts.md under "Mission Planner Agent".
/// </summary>
public class MissionPlannerOutput
{
    [JsonPropertyName("dispatchRequestId")]
    public string DispatchRequestId { get; set; } = string.Empty;

    [JsonPropertyName("plan")]
    public List<MissionPlanStep> Plan { get; set; } = new();

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;
}

/// <summary>
/// Individual step within the mission plan checklist.
/// </summary>
public class MissionPlanStep
{
    [JsonPropertyName("stepNumber")]
    public int StepNumber { get; set; }

    [JsonPropertyName("stepName")]
    public string StepName { get; set; } = string.Empty;

    /// <summary>
    /// Lifecycle state of the step (e.g. "Pending", "InProgress", "Completed").
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// The agent responsible for executing this step (e.g. "DispatchTelemetryAgent", "SafetyGuardAgent").
    /// </summary>
    [JsonPropertyName("assignedAgent")]
    public string? AssignedAgent { get; set; }
}
