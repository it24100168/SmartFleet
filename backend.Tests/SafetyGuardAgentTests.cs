using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SmartFleet.Backend.Agents.SafetyGuardAgent;
using SmartFleet.Backend.Agents.SafetyGuardAgent.DTOs;
using SmartFleet.Backend.Data;
using SmartFleet.Backend.Models.Enums;
using Xunit;

namespace SmartFleet.Backend.Tests;

public class SafetyGuardAgentTests
{
    private SmartFleetDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<SmartFleetDbContext>()
            .UseInMemoryDatabase(databaseName: $"SmartFleet_Test_{Guid.NewGuid()}")
            .Options;

        return new SmartFleetDbContext(options);
    }

    [Fact]
    public async Task EvaluateSafetyAsync_LowRisk_ShouldAutoApprove()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var agent = new SafetyGuardAgent(dbContext, NullLogger<SafetyGuardAgent>.Instance);

        var input = new SafetyGuardInput
        {
            DispatchRequestId = "d3f1-892a",
            RoverId = "RO-04",
            MissionPlanSummary = new MissionPlanSummary
            {
                Plan = new List<PlanStepSummary>
                {
                    new() { StepNumber = 1, StepName = "Locate available rover", Status = "Completed" }
                }
            },
            TelemetryResult = new TelemetryResultSummary
            {
                BatteryOk = true,
                WeatherRisk = "low",
                Locked = true
            },
            MaintenanceResult = null
        };

        // Act
        var output = await agent.EvaluateSafetyAsync(input);

        // Assert contract fields and values
        Assert.Equal("d3f1-892a", output.DispatchRequestId);
        Assert.True(output.RiskScore < 30, $"RiskScore was {output.RiskScore}, expected < 30");
        Assert.False(output.RequiresApproval);
        Assert.Equal("AutoApproved", output.AutoOutcome);
        Assert.Contains("sufficient battery", output.RiskReason, StringComparison.OrdinalIgnoreCase);

        // Verify no pending approval request was created
        var approvalCount = await dbContext.ApprovalRequests.CountAsync();
        Assert.Equal(0, approvalCount);

        // Verify execution log was recorded
        var log = await dbContext.WorkflowExecutionLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal("SafetyGuardEvaluation", log.StepName);
        Assert.Equal("SafetyGuardAgent", log.AgentName);
        Assert.Equal("AutoApproved", log.ValidationResult);
    }

    [Fact]
    public async Task EvaluateSafetyAsync_MediumRisk_ShouldTriggerPendingApprovalAndPause()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var agent = new SafetyGuardAgent(dbContext, NullLogger<SafetyGuardAgent>.Instance);

        var input = new SafetyGuardInput
        {
            DispatchRequestId = "d3f1-892a",
            RoverId = "RO-04",
            MissionPlanSummary = new MissionPlanSummary
            {
                Plan = new List<PlanStepSummary>
                {
                    new() { StepNumber = 1, StepName = "Locate available rover", Status = "Completed" }
                }
            },
            TelemetryResult = new TelemetryResultSummary
            {
                BatteryOk = true,
                WeatherRisk = "medium", // adds 35
                Locked = true
            },
            MaintenanceResult = new MaintenanceResultSummary
            {
                BreakdownReportId = "b9a1-1209",
                LikelyPart = "Proximity Sensor Array",
                EstimatedRepairHours = 1,
                Severity = "Medium", // adds 25 -> total 65
                ConfidenceNote = "Telemetry drift detected",
                RecommendedAction = "ScheduleRepair"
            }
        };

        // Act
        var output = await agent.EvaluateSafetyAsync(input);

        // Assert contract fields and values
        Assert.Equal("d3f1-892a", output.DispatchRequestId);
        Assert.True(output.RiskScore >= 30 && output.RiskScore < 75, $"RiskScore was {output.RiskScore}, expected between 30 and 74");
        Assert.True(output.RequiresApproval);
        Assert.Equal("PendingApproval", output.AutoOutcome);

        // Verify Human-in-the-Loop pause: ApprovalRequest row with status Pending created
        var approval = await dbContext.ApprovalRequests.FirstOrDefaultAsync();
        Assert.NotNull(approval);
        Assert.Equal("d3f1-892a", approval.DispatchRequestId);
        Assert.Equal("RO-04", approval.RoverId);
        Assert.Equal(ApprovalStatus.Pending, approval.Status);
        Assert.Equal(output.RiskScore, approval.RiskScore);
        Assert.Equal(output.RiskReason, approval.RiskReason);
        Assert.NotEmpty(approval.AgentSummaryJson);
    }

    [Fact]
    public async Task EvaluateSafetyAsync_CriticalRisk_ShouldAutoReject()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var agent = new SafetyGuardAgent(dbContext, NullLogger<SafetyGuardAgent>.Instance);

        var input = new SafetyGuardInput
        {
            DispatchRequestId = "d3f1-892a",
            RoverId = "RO-04",
            MissionPlanSummary = new MissionPlanSummary
            {
                Plan = new List<PlanStepSummary>
                {
                    new() { StepNumber = 1, StepName = "Locate available rover", Status = "Completed" }
                }
            },
            TelemetryResult = new TelemetryResultSummary
            {
                BatteryOk = false,
                WeatherRisk = "high",
                Locked = false
            },
            MaintenanceResult = new MaintenanceResultSummary
            {
                BreakdownReportId = "b7c2-441e",
                LikelyPart = "Drive Motor Unit",
                EstimatedRepairHours = 4,
                Severity = "High",
                ConfidenceNote = "Motor overheating detected",
                RecommendedAction = "ScheduleRepair"
            }
        };

        // Act
        var output = await agent.EvaluateSafetyAsync(input);

        // Assert contract fields and values
        Assert.Equal("d3f1-892a", output.DispatchRequestId);
        Assert.True(output.RiskScore >= 75, $"RiskScore was {output.RiskScore}, expected >= 75");
        Assert.False(output.RequiresApproval);
        Assert.Equal("AutoRejected", output.AutoOutcome);

        // AutoRejected does not create a pending approval request (mission rejected automatically)
        var approvalCount = await dbContext.ApprovalRequests.CountAsync();
        Assert.Equal(0, approvalCount);

        // Audit log exists
        var log = await dbContext.WorkflowExecutionLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal("AutoRejected", log.ValidationResult);
    }

    [Fact]
    public void ContractSerialization_ShouldMatchExactFieldNamesAndCasing()
    {
        // Assert exact JSON contract field names and casing per docs/agent-contracts.md
        var output = new SafetyGuardOutput
        {
            DispatchRequestId = "d3f1-892a",
            RiskScore = 12,
            RiskReason = "Low weather risk, sufficient battery, no open maintenance flags",
            RequiresApproval = false,
            AutoOutcome = "AutoApproved"
        };

        var json = JsonSerializer.Serialize(output);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Verify exact contract field names are present
        Assert.True(root.TryGetProperty("dispatchRequestId", out var dId));
        Assert.Equal("d3f1-892a", dId.GetString());

        Assert.True(root.TryGetProperty("riskScore", out var rScore));
        Assert.Equal(12, rScore.GetInt32());

        Assert.True(root.TryGetProperty("riskReason", out var rReason));
        Assert.Equal("Low weather risk, sufficient battery, no open maintenance flags", rReason.GetString());

        Assert.True(root.TryGetProperty("requiresApproval", out var reqApp));
        Assert.False(reqApp.GetBoolean());

        Assert.True(root.TryGetProperty("autoOutcome", out var outcome));
        Assert.Equal("AutoApproved", outcome.GetString());

        // Verify no incorrect casings exist
        Assert.False(root.TryGetProperty("RiskScore", out _));
        Assert.False(root.TryGetProperty("DispatchRequestId", out _));
        Assert.False(root.TryGetProperty("AutoOutcome", out _));
    }
}
