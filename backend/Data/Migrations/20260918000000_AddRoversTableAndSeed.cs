using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartFleet.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRoversTableAndSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Rovers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Identifier = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BatteryPercentage = table.Column<int>(type: "integer", nullable: false),
                    LocationZone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CurrentMissionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rovers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rovers_Identifier",
                table: "Rovers",
                column: "Identifier",
                unique: true);

            migrationBuilder.InsertData(
                table: "Rovers",
                columns: new[] { "Id", "BatteryPercentage", "CreatedAt", "CurrentMissionId", "Identifier", "LocationZone", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), 95, new DateTime(2026, 9, 18, 0, 0, 0, 0, DateTimeKind.Utc), null, "RO-01", "WarehouseA-DockA1", "Idle", new DateTime(2026, 9, 18, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-2222-2222-2222-222222222222"), 35, new DateTime(2026, 9, 18, 0, 0, 0, 0, DateTimeKind.Utc), null, "RO-02", "WarehouseA-ChargingBay", "Charging", new DateTime(2026, 9, 18, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("33333333-3333-3333-3333-333333333333"), 60, new DateTime(2026, 9, 18, 0, 0, 0, 0, DateTimeKind.Utc), "d1a0-554b", "RO-03", "WarehouseA-Aisle4", "Dispatched", new DateTime(2026, 9, 18, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("44444444-4444-4444-4444-444444444444"), 88, new DateTime(2026, 9, 18, 0, 0, 0, 0, DateTimeKind.Utc), null, "RO-04", "WarehouseA-DockA1", "Idle", new DateTime(2026, 9, 18, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("55555555-5555-5555-5555-555555555555"), 15, new DateTime(2026, 9, 18, 0, 0, 0, 0, DateTimeKind.Utc), null, "RO-05", "WarehouseA-MaintenanceArea", "Faulted", new DateTime(2026, 9, 18, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("66666666-6666-6666-6666-666666666666"), 22, new DateTime(2026, 9, 18, 0, 0, 0, 0, DateTimeKind.Utc), null, "RO-06", "WarehouseA-DockB3", "Idle", new DateTime(2026, 9, 18, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Rovers");
        }
    }
}
