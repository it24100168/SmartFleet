using System.Text.Json.Serialization;

namespace SmartFleet.Backend.Agents.MaintenanceMechanicAgent.DTOs;

/// <summary>
/// Strict contract output shape defined in docs/agent-contracts.md for the Maintenance Mechanic Agent.
/// </summary>
public class MaintenanceMechanicOutput
{
    [JsonPropertyName("breakdownReportId")]
    public string BreakdownReportId { get; set; } = string.Empty;

    [JsonPropertyName("likelyPart")]
    public string LikelyPart { get; set; } = string.Empty;

    [JsonPropertyName("estimatedRepairHours")]
    public int EstimatedRepairHours { get; set; }

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;

    [JsonPropertyName("confidenceNote")]
    public string ConfidenceNote { get; set; } = string.Empty;

    [JsonPropertyName("recommendedAction")]
    public string RecommendedAction { get; set; } = string.Empty;
}
