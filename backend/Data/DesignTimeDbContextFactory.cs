using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace SmartFleet.Backend.Data;
// Schema generation never starts the API or connects to the application database.
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SmartFleetDbContext>
{
    public SmartFleetDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<SmartFleetDbContext>()
        .UseNpgsql("Host=localhost;Database=smartfleet_schema_only;Username=postgres;Password=unused").Options);
}
