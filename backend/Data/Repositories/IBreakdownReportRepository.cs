using SmartFleet.Backend.Models;
using SmartFleet.Backend.Models.Enums;

namespace SmartFleet.Backend.Data.Repositories;

public interface IBreakdownReportRepository
{
    Task<BreakdownReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BreakdownReport?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<BreakdownReport> Items, int TotalCount)> GetPagedAsync(
        BreakdownStatus? status,
        string? symptomCategory,
        Guid? reportedById,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(BreakdownReport report, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
