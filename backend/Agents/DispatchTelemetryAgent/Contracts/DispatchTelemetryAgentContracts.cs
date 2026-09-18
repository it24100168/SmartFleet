using System.Text.Json.Serialization;

namespace SmartFleet.Backend.Agents.DispatchTelemetryAgent.Contracts;

/// <summary>
/// Exact input shape locked by team agreement in docs/agent-contracts.md
/// Downstream receiver: Dispatch & Telemetry Agent (owner: Hamdhan)
/// Upstream provider: Mission Planner Agent (owner: Chathumini)
/// </summary>
public class DispatchTelemetryAgentInput
{
    [JsonPropertyName("dispatchRequestId")]
    public string DispatchRequestId { get; set; } = string.Empty;

    [JsonPropertyName("planSteps")]
    public List<PlanStepDto> PlanSteps { get; set; } = new();

    [JsonPropertyName("sourceZone")]
    public string SourceZone { get; set; } = string.Empty;

    [JsonPropertyName("destinationZone")]
    public string DestinationZone { get; set; } = string.Empty;
}

public class PlanStepDto
{
    [JsonPropertyName("stepNumber")]
    public int StepNumber { get; set; }

    [JsonPropertyName("stepName")]
    public string StepName { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Pending";
}

/// <summary>
/// Exact output shape locked by team agreement in docs/agent-contracts.md
/// Downstream consumer: Safety Guard Agent (owner: Dilukshi)
/// </summary>
public class DispatchTelemetryAgentOutput
{
    [JsonPropertyName("dispatchRequestId")]
    public string DispatchRequestId { get; set; } = string.Empty;

    [JsonPropertyName("selectedRoverId")]
    public string? SelectedRoverId { get; set; }

    [JsonPropertyName("batteryOk")]
    public bool BatteryOk { get; set; }

    /// <summary>
    /// Exact casing: "low", "medium", or "high"
    /// </summary>
    [JsonPropertyName("weatherRisk")]
    public string WeatherRisk { get; set; } = "low";

    [JsonPropertyName("locked")]
    public bool Locked { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}
