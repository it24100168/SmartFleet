using System.Text.Json.Serialization;
using SmartFleet.Backend.Models.Enums;

namespace SmartFleet.Backend.Models;

/// <summary>
/// Represents a human-in-the-loop safety approval request created when the Safety Guard agent
/// detects risks requiring supervisor sign-off before a dispatch can proceed.
/// </summary>
public class ApprovalRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Identifier of the linked dispatch request (unconstrained column; real table lives in teammate's branch).
    /// </summary>
    public string DispatchRequestId { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the assigned rover (unconstrained column; real table lives in teammate's branch).
    /// </summary>
    public string RoverId { get; set; } = string.Empty;

    /// <summary>
    /// The assembled mission plan, telemetry results, and maintenance diagnostics as JSON.
    /// </summary>
    public string AgentSummaryJson { get; set; } = string.Empty;

    /// <summary>
    /// Risk score computed by the Safety Guard Agent (0 - 100).
    /// </summary>
    [JsonPropertyName("riskScore")]
    public int RiskScore { get; set; }

    /// <summary>
    /// Human-readable reasoning for the computed risk score.
    /// </summary>
    [JsonPropertyName("riskReason")]
    public string RiskReason { get; set; } = string.Empty;

    /// <summary>
    /// Lifecycle status of this approval request.
    /// </summary>
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    /// <summary>
    /// Foreign key of the supervisor who reviewed this request.
    /// </summary>
    public Guid? ReviewedById { get; set; }

    /// <summary>
    /// Navigation property to the reviewing user.
    /// </summary>
    public User? ReviewedBy { get; set; }

    /// <summary>
    /// Notes or feedback left by the supervisor upon review.
    /// </summary>
    public string? ReviewNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
