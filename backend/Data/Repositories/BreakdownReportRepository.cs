using Microsoft.EntityFrameworkCore;
using SmartFleet.Backend.Models;
using SmartFleet.Backend.Models.Enums;

namespace SmartFleet.Backend.Data.Repositories;

public class BreakdownReportRepository : IBreakdownReportRepository
{
    private readonly SmartFleetDbContext _context;

    public BreakdownReportRepository(SmartFleetDbContext context)
    {
        _context = context;
    }

    public async Task<BreakdownReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.BreakdownReports
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<BreakdownReport?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.BreakdownReports
            .Include(b => b.ReportedBy)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<BreakdownReport> Items, int TotalCount)> GetPagedAsync(
        BreakdownStatus? status,
        string? symptomCategory,
        Guid? reportedById,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.BreakdownReports
            .Include(b => b.ReportedBy)
            .AsNoTracking()
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(symptomCategory))
        {
            query = query.Where(b => b.SymptomCategory == symptomCategory);
        }

        if (reportedById.HasValue)
        {
            query = query.Where(b => b.ReportedById == reportedById.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(BreakdownReport report, CancellationToken cancellationToken = default)
    {
        await _context.BreakdownReports.AddAsync(report, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
