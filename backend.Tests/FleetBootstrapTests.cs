using Microsoft.EntityFrameworkCore;
using SmartFleet.Backend.Data;
using SmartFleet.Backend.Models;
using SmartFleet.Backend.Models.Enums;
using SmartFleet.Backend.Services;
using Xunit;

namespace SmartFleet.Backend.Tests;

public class FleetBootstrapTests
{
    [Fact]
    public async Task Clears_only_the_known_orphaned_seed_mission()
    {
        var options = new DbContextOptionsBuilder<SmartFleetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new SmartFleetDbContext(options);
        db.Rovers.AddRange(
            new Rover { Identifier = "RO-03", Status = RoverStatus.Dispatched, CurrentMissionId = "d1a0-554b" },
            new Rover { Identifier = "RO-07", Status = RoverStatus.Dispatched, CurrentMissionId = "real-mission" });
        await db.SaveChangesAsync();

        await FleetBootstrap.RepairLegacySeedAsync(db);
        await FleetBootstrap.RepairLegacySeedAsync(db);

        var oldSeed = await db.Rovers.SingleAsync(x => x.Identifier == "RO-03");
        var active = await db.Rovers.SingleAsync(x => x.Identifier == "RO-07");
        Assert.Equal(RoverStatus.Idle, oldSeed.Status);
        Assert.Null(oldSeed.CurrentMissionId);
        Assert.Equal(RoverStatus.Dispatched, active.Status);
        Assert.Equal("real-mission", active.CurrentMissionId);
    }
}
