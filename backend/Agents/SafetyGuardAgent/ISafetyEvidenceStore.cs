using SmartFleet.Backend.Models;

namespace SmartFleet.Backend.Agents.SafetyGuardAgent;

/// <summary>Only the approval and audit operations the Safety Guard may request.</summary>
public interface ISafetyEvidenceStore
{
    Task<bool> HasApprovalForWorkflowAsync(Guid workflowRunId, CancellationToken ct);
    Task SaveEvaluationAsync(ApprovalRequest? approval, WorkflowExecutionLog log, CancellationToken ct);
}
