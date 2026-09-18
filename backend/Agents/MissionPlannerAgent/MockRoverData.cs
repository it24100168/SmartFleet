namespace SmartFleet.Backend.Agents.MissionPlannerAgent;

/// <summary>
/// Mock rover representation for standalone development and testing prior to integration with teammate's Rover module.
/// </summary>
public record MockRover(string Id, int BatteryLevel, string Zone, bool IsAvailable);

public static class MockRoverData
{
    // TODO: Replace with real Rover service call when teammate's Rover module is integrated
    public static readonly IReadOnlyList<MockRover> Rovers = new List<MockRover>
    {
        new("RO-01", 85, "WarehouseA-DockA1", true),
        new("RO-02", 42, "WarehouseB-Storage", true),
        new("RO-03", 15, "WarehouseA-DockB3", false)
    };
}
