using SmartFleet.Backend.Models;

namespace SmartFleet.Backend.Data.Repositories;

public interface IFailureCatalogRepository
{
    Task<IReadOnlyList<FailureCatalog>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<FailureCatalog?> FindMatchAsync(string symptomCategory, string description, string? errorCode, CancellationToken cancellationToken = default);
}
