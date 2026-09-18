using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartFleet.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchRequestAndWorkflowRun : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DispatchRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoverId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceZone = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DestinationZone = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CargoType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PreferredTimeWindow = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchRequests_Users_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DispatchRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectiveJson = table.Column<string>(type: "text", nullable: false),
                    PlanJson = table.Column<string>(type: "text", nullable: false),
                    CurrentStep = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowRuns_DispatchRequests_DispatchRequestId",
                        column: x => x.DispatchRequestId,
                        principalTable: "DispatchRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRequests_CreatedAt",
                table: "DispatchRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRequests_OperatorId",
                table: "DispatchRequests",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRequests_Status",
                table: "DispatchRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRuns_DispatchRequestId",
                table: "WorkflowRuns",
                column: "DispatchRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowRuns");

            migrationBuilder.DropTable(
                name: "DispatchRequests");
        }
    }
}
