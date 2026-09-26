namespace SmartFleet.Backend.Services;
public static class WarehouseLayout
{
    public record Zone(string Id, string Label, double X, double Y);
    public static readonly Zone[] Zones =
    {
        new("WarehouseA-DockA1", "Receiving · A1", 15, 28),
        new("WarehouseA-DockB3", "Dispatch · B3", 85, 28),
        new("WarehouseA-Aisle4", "Storage · Aisle 4", 50, 72),
        new("WarehouseA-ChargingBay", "Charging", 15, 82),
        new("WarehouseA-MaintenanceArea", "Maintenance", 85, 82)
    };
    public static int RequiredBattery(string source, string destination)
    {
        var a = Zones.FirstOrDefault(z => z.Id == source);
        var b = Zones.FirstOrDefault(z => z.Id == destination);
        return a == null || b == null ? 40 : 20 + (int)Math.Ceiling((Math.Abs(a.X-b.X)+Math.Abs(a.Y-52)+Math.Abs(b.Y-52))/5);
    }
}
