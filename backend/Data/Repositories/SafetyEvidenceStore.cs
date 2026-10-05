using Microsoft.EntityFrameworkCore;
using SmartFleet.Backend.Agents.SafetyGuardAgent;
using SmartFleet.Backend.Models;

namespace SmartFleet.Backend.Data.Repositories;

public sealed class SafetyEvidenceStore(SmartFleetDbContext db) : ISafetyEvidenceStore
{
    public Task<bool> HasApprovalForWorkflowAsync(Guid workflowRunId, CancellationToken ct) =>
        db.ApprovalRequests.AnyAsync(x => x.WorkflowRunId == workflowRunId, ct);

    public async Task SaveEvaluationAsync(ApprovalRequest? approval, WorkflowExecutionLog log, CancellationToken ct)
    {
        if (approval != null) db.ApprovalRequests.Add(approval);
        db.WorkflowExecutionLogs.Add(log);
        await db.SaveChangesAsync(ct);
    }
}
