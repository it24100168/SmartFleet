using System.Text.Json.Serialization;
using SmartFleet.Backend.Models.Enums;

namespace SmartFleet.Backend.DTOs.Approvals;

public class ApprovalRequestDto
{
    public Guid Id { get; set; }
    public string DispatchRequestId { get; set; } = string.Empty;
    public string RoverId { get; set; } = string.Empty;
    public string AgentSummaryJson { get; set; } = string.Empty;

    [JsonPropertyName("riskScore")]
    public int RiskScore { get; set; }

    [JsonPropertyName("riskReason")]
    public string RiskReason { get; set; } = string.Empty;

    public ApprovalStatus Status { get; set; }
    public Guid? ReviewedById { get; set; }
    public string? ReviewedByName { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ApprovalDecisionDto
{
    public string? ReviewNotes { get; set; }
}

public class PagedApprovalResultDto
{
    public List<ApprovalRequestDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / (PageSize > 0 ? PageSize : 10));
}

public class WorkflowExecutionLogDto
{
    public Guid Id { get; set; }
    public string DispatchRequestId { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public string InputJson { get; set; } = string.Empty;
    public string OutputJson { get; set; } = string.Empty;
    public string ValidationResult { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class ApprovalStatsDto
{
    public int TotalPending { get; set; }
    public int TotalApproved { get; set; }
    public int TotalRejected { get; set; }
    public int TotalRevisionRequested { get; set; }
    public double AverageRiskScore { get; set; }
}

public class SimulateEvaluationDto
{
    public string Scenario { get; set; } = "pending-approval"; // "low-risk", "pending-approval", "auto-reject"
    public string? DispatchRequestId { get; set; }
    public string? RoverId { get; set; }
}
