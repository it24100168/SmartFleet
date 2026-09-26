using SmartFleet.Backend.Models;

namespace SmartFleet.Backend.Data.Repositories;

public interface IWorkflowRunRepository
{
    Task<WorkflowRun?> GetLatestByDispatchRequestIdAsync(Guid dispatchRequestId, CancellationToken cancellationToken = default);
    Task AddAsync(WorkflowRun workflowRun, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
