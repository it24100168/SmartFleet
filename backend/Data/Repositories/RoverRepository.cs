using System.Data;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Backend.Models;
using SmartFleet.Backend.Models.Enums;

namespace SmartFleet.Backend.Data.Repositories;

public class RoverRepository : IRoverRepository
{
    private readonly SmartFleetDbContext _context;

    public RoverRepository(SmartFleetDbContext context)
    {
        _context = context;
    }

    public async Task<(List<Rover> Items, int TotalCount)> GetPagedAsync(
        RoverStatus? status,
        string? zone,
        string? sortBy,
        bool isDescending,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Rovers.AsNoTracking().AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(zone))
        {
            var normalizedZone = zone.Trim().ToLowerInvariant();
            query = query.Where(r => r.LocationZone.ToLower().Contains(normalizedZone));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Sorting
        query = (sortBy?.ToLowerInvariant()) switch
        {
            "battery" => isDescending ? query.OrderByDescending(r => r.BatteryPercentage) : query.OrderBy(r => r.BatteryPercentage),
            "status" => isDescending ? query.OrderByDescending(r => r.Status) : query.OrderBy(r => r.Status),
            "zone" => isDescending ? query.OrderByDescending(r => r.LocationZone) : query.OrderBy(r => r.LocationZone),
            _ => isDescending ? query.OrderByDescending(r => r.Identifier) : query.OrderBy(r => r.Identifier),
        };

        var pageIndex = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);

        var items = await query
            .Skip((pageIndex - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Rover?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Rovers
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Rover?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var normalized = identifier.Trim().ToLowerInvariant();
        return await _context.Rovers
            .FirstOrDefaultAsync(r => r.Identifier.ToLower() == normalized, cancellationToken);
    }

    public async Task<List<Rover>> GetAvailableRoversInZoneAsync(string zone, int minBattery, CancellationToken cancellationToken = default)
    {
        var normalizedZone = zone.Trim().ToLowerInvariant();

        return await _context.Rovers
            .Where(r => r.Status == RoverStatus.Idle
                     && r.BatteryPercentage >= minBattery
                     && r.LocationZone.ToLower() == normalizedZone)
            .OrderByDescending(r => r.BatteryPercentage)
            .ToListAsync(cancellationToken);
    }

    public async Task<Rover?> LockRoverForMissionAsync(Guid roverId, string missionId, CancellationToken cancellationToken = default)
    {
        // Execute inside an isolated database transaction to guarantee no double-booking
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                var rover = await _context.Rovers
                    .FirstOrDefaultAsync(r => r.Id == roverId, cancellationToken);

                if (rover == null)
                {
                    return null;
                }

                if (rover.Status != RoverStatus.Idle)
                {
                    throw new InvalidOperationException($"Rover '{rover.Identifier}' is already locked or in non-idle state ({rover.Status}).");
                }

                rover.Status = RoverStatus.Dispatched;
                rover.CurrentMissionId = missionId;
                rover.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return rover;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task UpdateAsync(Rover rover, CancellationToken cancellationToken = default)
    {
        rover.UpdatedAt = DateTime.UtcNow;
        _context.Rovers.Update(rover);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
