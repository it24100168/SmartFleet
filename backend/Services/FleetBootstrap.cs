using Microsoft.EntityFrameworkCore;
using SmartFleet.Backend.Data;
using SmartFleet.Backend.Models.Enums;

namespace SmartFleet.Backend.Services;

/// <summary>Removes a legacy sample mission marker from the migration seed.</summary>
public static class FleetBootstrap
{
    public static async Task RepairLegacySeedAsync(SmartFleetDbContext db, CancellationToken ct = default)
    {
        var rover = await db.Rovers.SingleOrDefaultAsync(r => r.Identifier == "RO-03"
            && r.CurrentMissionId == "d1a0-554b" && r.Status == RoverStatus.Dispatched, ct);
        if (rover == null) return;
        rover.Status = RoverStatus.Idle;
        rover.CurrentMissionId = null;
        rover.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
