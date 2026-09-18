using SmartFleet.Backend.Models;
using SmartFleet.Backend.Models.Enums;

namespace SmartFleet.Backend.Data.Repositories;

public interface IRoverRepository
{
    Task<(List<Rover> Items, int TotalCount)> GetPagedAsync(
        RoverStatus? status,
        string? zone,
        string? sortBy,
        bool isDescending,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Rover?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Rover?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);

    Task<List<Rover>> GetAvailableRoversInZoneAsync(string zone, int minBattery, CancellationToken cancellationToken = default);

    Task<Rover?> LockRoverForMissionAsync(Guid roverId, string missionId, CancellationToken cancellationToken = default);

    Task UpdateAsync(Rover rover, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
