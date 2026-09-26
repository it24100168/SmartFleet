using Microsoft.EntityFrameworkCore;
using SmartFleet.Backend.DTOs.Common;
using SmartFleet.Backend.DTOs.Dispatch;
using SmartFleet.Backend.Models;

namespace SmartFleet.Backend.Data.Repositories;

public class DispatchRequestRepository : IDispatchRequestRepository
{
    private readonly SmartFleetDbContext _context;

    public DispatchRequestRepository(SmartFleetDbContext context)
    {
        _context = context;
    }

    public async Task<DispatchRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.DispatchRequests
            .Include(d => d.Operator)
            .Include(d => d.WorkflowRuns.OrderByDescending(w => w.CreatedAt))
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<PagedResultDto<DispatchRequest>> GetPagedAsync(
        DispatchRequestQueryDto query,
        Guid? operatorIdFilter = null,
        CancellationToken cancellationToken = default)
    {
        var queryable = _context.DispatchRequests
            .Include(d => d.Operator)
            .Include(d => d.WorkflowRuns.OrderByDescending(w => w.CreatedAt))
            .AsNoTracking();

        // Role-based filter (e.g. Operators see only their own)
        if (operatorIdFilter.HasValue)
        {
            queryable = queryable.Where(d => d.OperatorId == operatorIdFilter.Value);
        }

        // Status filter
        if (query.Status.HasValue)
        {
            queryable = queryable.Where(d => d.Status == query.Status.Value);
        }

        // Zone filter (matches source or destination)
        if (!string.IsNullOrWhiteSpace(query.Zone))
        {
            var zone = query.Zone.Trim().ToLower();
            queryable = queryable.Where(d => d.SourceZone.ToLower().Contains(zone) || d.DestinationZone.ToLower().Contains(zone));
        }

        // General search filter
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            queryable = queryable.Where(d =>
                d.SourceZone.ToLower().Contains(search) ||
                d.DestinationZone.ToLower().Contains(search) ||
                d.CargoType.ToLower().Contains(search) ||
                d.Priority.ToLower().Contains(search) ||
                (d.Operator != null && d.Operator.Name.ToLower().Contains(search)));
        }

        // Total count before pagination
        var totalCount = await queryable.CountAsync(cancellationToken);

        // Sorting
        queryable = (query.SortBy?.ToLower(), query.SortDescending) switch
        {
            ("priority", true) => queryable.OrderByDescending(d => d.Priority).ThenByDescending(d => d.CreatedAt),
            ("priority", false) => queryable.OrderBy(d => d.Priority).ThenByDescending(d => d.CreatedAt),
            ("status", true) => queryable.OrderByDescending(d => d.Status).ThenByDescending(d => d.CreatedAt),
            ("status", false) => queryable.OrderBy(d => d.Status).ThenByDescending(d => d.CreatedAt),
            ("preferredtimewindow", true) => queryable.OrderByDescending(d => d.PreferredTimeWindow),
            ("preferredtimewindow", false) => queryable.OrderBy(d => d.PreferredTimeWindow),
            ("createdat", false) => queryable.OrderBy(d => d.CreatedAt),
            _ => queryable.OrderByDescending(d => d.CreatedAt)
        };

        // Pagination
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var items = await queryable
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<DispatchRequest>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task AddAsync(DispatchRequest request, CancellationToken cancellationToken = default)
    {
        await _context.DispatchRequests.AddAsync(request, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
