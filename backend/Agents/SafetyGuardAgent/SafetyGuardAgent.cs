using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Backend.Agents.SafetyGuardAgent.DTOs;
using SmartFleet.Backend.Data;
using SmartFleet.Backend.Models;
using SmartFleet.Backend.Models.Enums;

namespace SmartFleet.Backend.Agents.SafetyGuardAgent;

/// <summary>
/// Safety Guard Agent - Enforces deterministic warehouse operating constraints and triggers
/// human-in-the-loop pauses when risks exceed safe operational thresholds.
/// </summary>
public class SafetyGuardAgent : ISafetyGuardAgent
{
    private readonly SmartFleetDbContext _dbContext;
    private readonly ILogger<SafetyGuardAgent> _logger;

    public SafetyGuardAgent(SmartFleetDbContext dbContext, ILogger<SafetyGuardAgent> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Evaluates the aggregated input against deterministic safety rules.
    /// Strictly adheres to the contract in docs/agent-contracts.md.
    /// </summary>
    public async Task<SafetyGuardOutput> EvaluateSafetyAsync(SafetyGuardInput input, CancellationToken cancellationToken = default)
    {
        // TODO: [Integration Week] Replace mocked input with aggregated pipeline output from MissionPlannerAgent, DispatchTelemetryAgent, and MaintenanceMechanicAgent
        ArgumentNullException.ThrowIfNull(input);

        _logger.LogInformation(
            "Evaluating safety bounds for DispatchRequestId: {DispatchId}, RoverId: {RoverId}",
            input.DispatchRequestId,
            input.RoverId
        );

        int calculatedRisk = 0;
        var riskReasons = new List<string>();

        // 1. Weather risk evaluation (contract values: "low", "medium", "high")
        var weather = input.TelemetryResult?.WeatherRisk?.ToLowerInvariant() ?? "low";
        switch (weather)
        {
            case "high":
                calculatedRisk += 55;
                riskReasons.Add("Severe weather conditions detected");
                break;
            case "medium":
                calculatedRisk += 35;
                riskReasons.Add("Moderate weather risk across transit zone");
                break;
            case "low":
            default:
                calculatedRisk += 5;
                break;
        }

        // 2. Battery margin check
        if (input.TelemetryResult == null || !input.TelemetryResult.BatteryOk)
        {
            calculatedRisk += 40;
            riskReasons.Add("Insufficient battery reserve for mission profile");
        }

        // 3. Rover hardware lock verification
        if (input.TelemetryResult == null || !input.TelemetryResult.Locked)
        {
            calculatedRisk += 25;
            riskReasons.Add("Rover lock reservation unconfirmed");
        }

        // 4. Open breakdown report or maintenance alerts
        if (input.MaintenanceResult != null)
        {
            var severity = input.MaintenanceResult.Severity?.ToLowerInvariant();
            if (severity == "high" || severity == "critical")
            {
                calculatedRisk += 50;
                riskReasons.Add($"Active high-severity breakdown report ({input.MaintenanceResult.BreakdownReportId}: {input.MaintenanceResult.LikelyPart})");
            }
            else if (severity == "medium")
            {
                calculatedRisk += 25;
                riskReasons.Add($"Subsystem alert logged ({input.MaintenanceResult.BreakdownReportId}: {input.MaintenanceResult.LikelyPart})");
            }
            else
            {
                calculatedRisk += 10;
                riskReasons.Add($"Minor maintenance note recorded ({input.MaintenanceResult.BreakdownReportId})");
            }
        }

        // 5. Mission plan step integrity
        if (input.MissionPlanSummary?.Plan == null || input.MissionPlanSummary.Plan.Count == 0)
        {
            calculatedRisk += 20;
            riskReasons.Add("Empty mission plan provided");
        }

        // Bound risk score between 0 and 100
        int finalRiskScore = Math.Clamp(calculatedRisk, 0, 100);

        // Build human-readable risk reason
        string finalRiskReason;
        if (riskReasons.Count == 0)
        {
            finalRiskReason = "Low weather risk, sufficient battery, no open maintenance flags";
        }
        else
        {
            finalRiskReason = string.Join(", ", riskReasons);
        }

        // Determine deterministic auto-outcome
        string autoOutcome;
        bool requiresApproval;

        // AutoReject when critical risk thresholds or hardware failure combination is met
        bool isCriticalHardwareHazard = input.MaintenanceResult != null &&
            (string.Equals(input.MaintenanceResult.Severity, "High", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(input.MaintenanceResult.Severity, "Critical", StringComparison.OrdinalIgnoreCase));

        bool isCriticalDepletionAndWeather = (input.TelemetryResult != null && !input.TelemetryResult.BatteryOk && weather == "high");

        if (finalRiskScore >= 75 || isCriticalHardwareHazard || isCriticalDepletionAndWeather)
        {
            autoOutcome = "AutoRejected";
            requiresApproval = false;
        }
        else if (finalRiskScore >= 30)
        {
            autoOutcome = "PendingApproval";
            requiresApproval = true;
        }
        else
        {
            autoOutcome = "AutoApproved";
            requiresApproval = false;
        }

        var output = new SafetyGuardOutput
        {
            DispatchRequestId = input.DispatchRequestId,
            RiskScore = finalRiskScore,
            RiskReason = finalRiskReason,
            RequiresApproval = requiresApproval,
            AutoOutcome = autoOutcome
        };

        var inputJson = JsonSerializer.Serialize(input);
        var outputJson = JsonSerializer.Serialize(output);

        // Human-in-the-Loop Pause: If approval is required, create a pending ApprovalRequest row
        if (requiresApproval)
        {
            var approvalRequest = new ApprovalRequest
            {
                DispatchRequestId = input.DispatchRequestId,
                RoverId = input.RoverId,
                AgentSummaryJson = inputJson,
                RiskScore = finalRiskScore,
                RiskReason = finalRiskReason,
                Status = ApprovalStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.ApprovalRequests.Add(approvalRequest);
            _logger.LogWarning(
                "Safety Guard flagged DispatchRequest {DispatchId} for supervisor approval. RiskScore: {Score}. Reason: {Reason}",
                input.DispatchRequestId,
                finalRiskScore,
                finalRiskReason
            );
        }

        // Log auditable execution summary to WorkflowExecutionLog
        var executionLog = new WorkflowExecutionLog
        {
            DispatchRequestId = input.DispatchRequestId,
            StepName = "SafetyGuardEvaluation",
            AgentName = "SafetyGuardAgent",
            InputJson = inputJson,
            OutputJson = outputJson,
            ValidationResult = autoOutcome,
            Timestamp = DateTime.UtcNow
        };
        _dbContext.WorkflowExecutionLogs.Add(executionLog);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return output;
    }

    /// <summary>
    /// Helper to generate deterministic mocked input objects for testing and live pipeline demonstration.
    /// </summary>
    public SafetyGuardInput CreateMockedInput(string scenario = "low-risk", string? dispatchRequestId = null, string? roverId = null)
    {
        var dId = dispatchRequestId ?? $"d3f1-{Guid.NewGuid().ToString("N")[..4]}";
        var rId = roverId ?? "RO-04";

        switch (scenario.ToLowerInvariant())
        {
            case "high-risk":
            case "auto-reject":
            case "autorejected":
                return new SafetyGuardInput
                {
                    DispatchRequestId = dId,
                    RoverId = rId,
                    MissionPlanSummary = new MissionPlanSummary
                    {
                        Plan = new List<PlanStepSummary>
                        {
                            new() { StepNumber = 1, StepName = "Locate available rover", Status = "Completed" },
                            new() { StepNumber = 2, StepName = "Assign hazardous corridor", Status = "Pending" }
                        }
                    },
                    TelemetryResult = new TelemetryResultSummary
                    {
                        BatteryOk = false,
                        WeatherRisk = "high",
                        Locked = true
                    },
                    MaintenanceResult = new MaintenanceResultSummary
                    {
                        BreakdownReportId = "b7c2-441e",
                        LikelyPart = "Drive Motor Unit",
                        EstimatedRepairHours = 4,
                        Severity = "High",
                        ConfidenceNote = "Matched via FailureCatalog keyword: motor overheating",
                        RecommendedAction = "ScheduleRepair"
                    }
                };

            case "medium-risk":
            case "pending-approval":
            case "pendingapproval":
            case "pause":
                return new SafetyGuardInput
                {
                    DispatchRequestId = dId,
                    RoverId = rId,
                    MissionPlanSummary = new MissionPlanSummary
                    {
                        Plan = new List<PlanStepSummary>
                        {
                            new() { StepNumber = 1, StepName = "Locate available rover", Status = "Completed" },
                            new() { StepNumber = 2, StepName = "Cross Docking Hub C", Status = "Pending" }
                        }
                    },
                    TelemetryResult = new TelemetryResultSummary
                    {
                        BatteryOk = true,
                        WeatherRisk = "medium",
                        Locked = true
                    },
                    MaintenanceResult = new MaintenanceResultSummary
                    {
                        BreakdownReportId = "b9a1-1209",
                        LikelyPart = "Proximity Sensor Array",
                        EstimatedRepairHours = 1,
                        Severity = "Medium",
                        ConfidenceNote = "Sensor drift reported during calibration",
                        RecommendedAction = "ScheduleRepair"
                    }
                };

            case "low-risk":
            case "auto-approve":
            case "autoapproved":
            default:
                return new SafetyGuardInput
                {
                    DispatchRequestId = dId,
                    RoverId = rId,
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
        }
    }
}
