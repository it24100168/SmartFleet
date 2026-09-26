using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartFleet.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class IntegrateFleetWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyToken",
                table: "WorkflowRuns",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "WorkflowRuns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDemo",
                table: "WorkflowRuns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "Progress",
                table: "WorkflowRuns",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReservedUntil",
                table: "WorkflowRuns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RoverId",
                table: "WorkflowRuns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StartZone",
                table: "WorkflowRuns",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WeatherRisk",
                table: "WorkflowRuns",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "WorkflowRunId",
                table: "WorkflowExecutionLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyToken",
                table: "DispatchRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyToken",
                table: "ApprovalRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "WorkflowRunId",
                table: "ApprovalRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogs_WorkflowRunId_Timestamp",
                table: "WorkflowExecutionLogs",
                columns: new[] { "WorkflowRunId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_WorkflowRunId",
                table: "ApprovalRequests",
                column: "WorkflowRunId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkflowExecutionLogs_WorkflowRunId_Timestamp",
                table: "WorkflowExecutionLogs");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalRequests_WorkflowRunId",
                table: "ApprovalRequests");

            migrationBuilder.DropColumn(
                name: "ConcurrencyToken",
                table: "WorkflowRuns");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "WorkflowRuns");

            migrationBuilder.DropColumn(
                name: "IsDemo",
                table: "WorkflowRuns");

            migrationBuilder.DropColumn(
                name: "Progress",
                table: "WorkflowRuns");

            migrationBuilder.DropColumn(
                name: "ReservedUntil",
                table: "WorkflowRuns");

            migrationBuilder.DropColumn(
                name: "RoverId",
                table: "WorkflowRuns");

            migrationBuilder.DropColumn(
                name: "StartZone",
                table: "WorkflowRuns");

            migrationBuilder.DropColumn(
                name: "WeatherRisk",
                table: "WorkflowRuns");

            migrationBuilder.DropColumn(
                name: "WorkflowRunId",
                table: "WorkflowExecutionLogs");

            migrationBuilder.DropColumn(
                name: "ConcurrencyToken",
                table: "DispatchRequests");

            migrationBuilder.DropColumn(
                name: "ConcurrencyToken",
                table: "ApprovalRequests");

            migrationBuilder.DropColumn(
                name: "WorkflowRunId",
                table: "ApprovalRequests");
        }
    }
}
