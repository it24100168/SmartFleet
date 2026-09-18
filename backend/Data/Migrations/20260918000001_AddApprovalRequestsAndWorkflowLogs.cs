using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartFleet.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalRequestsAndWorkflowLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApprovalRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DispatchRequestId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RoverId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AgentSummaryJson = table.Column<string>(type: "text", nullable: false),
                    RiskScore = table.Column<int>(type: "integer", nullable: false),
                    RiskReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ReviewedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalRequests_Users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowExecutionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DispatchRequestId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StepName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AgentName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    InputJson = table.Column<string>(type: "text", nullable: false),
                    OutputJson = table.Column<string>(type: "text", nullable: false),
                    ValidationResult = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowExecutionLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_CreatedAt",
                table: "ApprovalRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_DispatchRequestId",
                table: "ApprovalRequests",
                column: "DispatchRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_ReviewedById",
                table: "ApprovalRequests",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_Status",
                table: "ApprovalRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogs_DispatchRequestId",
                table: "WorkflowExecutionLogs",
                column: "DispatchRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogs_Timestamp",
                table: "WorkflowExecutionLogs",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalRequests");

            migrationBuilder.DropTable(
                name: "WorkflowExecutionLogs");
        }
    }
}
