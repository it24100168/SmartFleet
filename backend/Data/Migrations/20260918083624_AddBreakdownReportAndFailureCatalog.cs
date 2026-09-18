using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartFleet.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBreakdownReportAndFailureCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BreakdownReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoverId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReportedById = table.Column<Guid>(type: "uuid", nullable: false),
                    SymptomCategory = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PhotoUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DiagnosisResultJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BreakdownReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BreakdownReports_Users_ReportedById",
                        column: x => x.ReportedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FailureCatalogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SymptomKeyword = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SymptomCategory = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LikelyPart = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EstimatedRepairHours = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FailureCatalogs", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "FailureCatalogs",
                columns: new[] { "Id", "EstimatedRepairHours", "LikelyPart", "Severity", "SymptomCategory", "SymptomKeyword" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111101"), 2, "Drive Motor Unit", "High", "MotorOverheating", "motor overheating" },
                    { new Guid("11111111-1111-1111-1111-111111111102"), 3, "Wheel Bearing & Axle Assembly", "High", "WheelJam", "wheel jam" },
                    { new Guid("11111111-1111-1111-1111-111111111103"), 2, "Drive Motor Unit", "High", "MotorOverheating", "grinding noise" },
                    { new Guid("11111111-1111-1111-1111-111111111104"), 1, "Front LiDAR / Ultrasonic Array", "Medium", "SensorFault", "sensor fault" },
                    { new Guid("11111111-1111-1111-1111-111111111105"), 4, "Lithium Iron Phosphate Battery Pack", "Critical", "BatteryDegradation", "battery degradation" },
                    { new Guid("11111111-1111-1111-1111-111111111106"), 1, "Telemetry Wi-Fi / UWB Module", "Low", "SensorFault", "communication loss" },
                    { new Guid("11111111-1111-1111-1111-111111111107"), 3, "Electromagnetic Brake Caliper", "Critical", "WheelJam", "brake failure" },
                    { new Guid("11111111-1111-1111-1111-111111111108"), 2, "Steering Actuator Servo", "High", "WheelJam", "steering lock" },
                    { new Guid("11111111-1111-1111-1111-111111111109"), 2, "Power Distribution Board (PDB)", "High", "BatteryDegradation", "overvoltage" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BreakdownReports_CreatedAt",
                table: "BreakdownReports",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BreakdownReports_ReportedById",
                table: "BreakdownReports",
                column: "ReportedById");

            migrationBuilder.CreateIndex(
                name: "IX_BreakdownReports_Status",
                table: "BreakdownReports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FailureCatalogs_SymptomKeyword",
                table: "FailureCatalogs",
                column: "SymptomKeyword");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BreakdownReports");

            migrationBuilder.DropTable(
                name: "FailureCatalogs");
        }
    }
}
