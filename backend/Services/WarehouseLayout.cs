namespace SmartFleet.Backend.Services;
public static class WarehouseLayout
{
    public static bool IsKnown(string zone) => Zones.Any(z => z.Id == zone);
    public static void ValidateRoute(string source, string destination)
    {
        if (!IsKnown(source) || !IsKnown(destination) || source == destination)
            throw new ArgumentException("Choose different pickup and delivery zones from the warehouse map.");
    }
    public record Zone(string Id, string Label, double X, double Y);
    public record Point(double X, double Y);
    public static Point[] Route(string start, string source, string destination)
    {
        Point At(string id) { var z = Zones.Single(z => z.Id == id); return new(z.X, z.Y); }
        var a = At(start); var b = At(source); var c = At(destination);
        var points = new List<Point> { a };
        void Leg(Point from, Point to)
        {
            if (from == to) return;
            points.Add(new(from.X, 52)); points.Add(new(to.X, 52)); points.Add(to);
        }
        Leg(a, b); Leg(b, c);
        return points.DistinctConsecutive().ToArray();
    }
    private static IEnumerable<Point> DistinctConsecutive(this IEnumerable<Point> points)
    {
        Point? previous = null;
        foreach (var point in points) { if (point != previous) yield return point; previous = point; }
    }
    public static Point Position(Point[] points, double progress)
    {
        double Length(Point a, Point b) => Math.Sqrt(Math.Pow(b.X-a.X, 2) + Math.Pow(b.Y-a.Y, 2));
        var remaining = points.Zip(points.Skip(1), Length).Sum() * Math.Clamp(progress, 0, 1);
        for (var i=1; i<points.Length; i++)
        {
            var length = Length(points[i-1], points[i]);
            if (length > 0 && remaining <= length)
            {
                var t = remaining / length;
                return new(points[i-1].X + (points[i].X-points[i-1].X)*t, points[i-1].Y + (points[i].Y-points[i-1].Y)*t);
            }
            remaining -= length;
        }
        return points[^1];
    }
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
