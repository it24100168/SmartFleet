using System.Text.Json.Serialization;

namespace SmartFleet.Backend.Agents.SafetyGuardAgent.DTOs;

/// <summary>
/// Safety Guard Agent Input Contract - strictly adheres to docs/agent-contracts.md.
/// </summary>
public class SafetyGuardInput
{
    [JsonPropertyName("dispatchRequestId")]
    public string DispatchRequestId { get; set; } = string.Empty;

    [JsonPropertyName("roverId")]
    public string RoverId { get; set; } = string.Empty;

    [JsonPropertyName("missionPlanSummary")]
    public MissionPlanSummary MissionPlanSummary { get; set; } = new();

    [JsonPropertyName("telemetryResult")]
    public TelemetryResultSummary TelemetryResult { get; set; } = new();

    [JsonPropertyName("maintenanceResult")]
    public MaintenanceResultSummary? MaintenanceResult { get; set; }
}

public class MissionPlanSummary
{
    [JsonPropertyName("plan")]
    public List<PlanStepSummary> Plan { get; set; } = new();
}

public class PlanStepSummary
{
    [JsonPropertyName("stepNumber")]
    public int StepNumber { get; set; }

    [JsonPropertyName("stepName")]
    public string StepName { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Pending";
}

public class TelemetryResultSummary
{
    [JsonPropertyName("batteryOk")]
    public bool BatteryOk { get; set; }

    /// <summary>
    /// Exact casing per contract: "low", "medium", "high"
    /// </summary>
    [JsonPropertyName("weatherRisk")]
    public string WeatherRisk { get; set; } = "low";

    [JsonPropertyName("locked")]
    public bool Locked { get; set; }
}

public class MaintenanceResultSummary
{
    [JsonPropertyName("breakdownReportId")]
    public string BreakdownReportId { get; set; } = string.Empty;

    [JsonPropertyName("likelyPart")]
    public string LikelyPart { get; set; } = string.Empty;

    [JsonPropertyName("estimatedRepairHours")]
    public double EstimatedRepairHours { get; set; }

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "Low";

    [JsonPropertyName("confidenceNote")]
    public string ConfidenceNote { get; set; } = string.Empty;

    [JsonPropertyName("recommendedAction")]
    public string RecommendedAction { get; set; } = string.Empty;
}

/// <summary>
/// Safety Guard Agent Output Contract - strictly adheres to docs/agent-contracts.md.
/// </summary>
public class SafetyGuardOutput
{
    [JsonPropertyName("dispatchRequestId")]
    public string DispatchRequestId { get; set; } = string.Empty;

    [JsonPropertyName("riskScore")]
    public int RiskScore { get; set; }

    [JsonPropertyName("riskReason")]
    public string RiskReason { get; set; } = string.Empty;

    [JsonPropertyName("requiresApproval")]
    public bool RequiresApproval { get; set; }

    /// <summary>
    /// Exact string values: "AutoApproved", "PendingApproval", "AutoRejected"
    /// </summary>
    [JsonPropertyName("autoOutcome")]
    public string AutoOutcome { get; set; } = string.Empty;
}
