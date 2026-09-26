namespace SmartFleet.Backend.Models;

/// <summary>
/// Audit trail entity recording inputs, outputs, and validation status for each step in an agentic workflow.
/// </summary>
public class WorkflowExecutionLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Identifier of the related dispatch request.
    /// </summary>
    public string DispatchRequestId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the execution step (e.g. MissionPlanning, TelemetryVerification, SafetyGuardEvaluation, SupervisorReview).
    /// </summary>
    public string StepName { get; set; } = string.Empty;

    /// <summary>
    /// Agent or actor performing this step (e.g. SafetyGuardAgent, MissionPlannerAgent, Supervisor).
    /// </summary>
    public string AgentName { get; set; } = string.Empty;

    /// <summary>
    /// Input payload in JSON format.
    /// </summary>
    public string InputJson { get; set; } = "{}";

    /// <summary>
    /// Output payload in JSON format.
    /// </summary>
    public string OutputJson { get; set; } = "{}";

    /// <summary>
    /// Validation result or outcome (e.g. AutoApproved, PendingApproval, AutoRejected, Approved, Rejected, RevisionRequested).
    /// </summary>
    public string ValidationResult { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when this log entry was recorded.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
