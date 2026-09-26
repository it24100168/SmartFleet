using SmartFleet.Backend.DTOs.Common;
using SmartFleet.Backend.DTOs.Dispatch;
using SmartFleet.Backend.Models;

namespace SmartFleet.Backend.Data.Repositories;

public interface IDispatchRequestRepository
{
    Task<DispatchRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResultDto<DispatchRequest>> GetPagedAsync(DispatchRequestQueryDto query, Guid? operatorIdFilter = null, CancellationToken cancellationToken = default);
    Task AddAsync(DispatchRequest request, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
