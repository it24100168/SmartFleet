using Microsoft.EntityFrameworkCore;
using SmartFleet.Backend.Models;

namespace SmartFleet.Backend.Data.Repositories;

public class WorkflowRunRepository : IWorkflowRunRepository
{
    private readonly SmartFleetDbContext _context;

    public WorkflowRunRepository(SmartFleetDbContext context)
    {
        _context = context;
    }

    public async Task<WorkflowRun?> GetLatestByDispatchRequestIdAsync(Guid dispatchRequestId, CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowRuns
            .Where(w => w.DispatchRequestId == dispatchRequestId)
            .OrderByDescending(w => w.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(WorkflowRun workflowRun, CancellationToken cancellationToken = default)
    {
        await _context.WorkflowRuns.AddAsync(workflowRun, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
