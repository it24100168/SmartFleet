using System.Text.Json.Serialization;

namespace SmartFleet.Backend.Agents.MaintenanceMechanicAgent.DTOs;

/// <summary>
/// Strict contract input shape defined in docs/agent-contracts.md for the Maintenance Mechanic Agent.
/// </summary>
public class MaintenanceMechanicInput
{
    [JsonPropertyName("breakdownReportId")]
    public string BreakdownReportId { get; set; } = string.Empty;

    [JsonPropertyName("symptomCategory")]
    public string SymptomCategory { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }
}
