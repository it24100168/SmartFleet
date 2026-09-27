using SmartFleet.Backend.Agents.MissionPlannerAgent;
using SmartFleet.Backend.Agents.DispatchTelemetryAgent.Contracts;
using SmartFleet.Backend.Agents.SafetyGuardAgent.DTOs;
using SmartFleet.Backend.Agents.MaintenanceMechanicAgent.DTOs;

namespace SmartFleet.Backend.Services;

// Executable actions are fixed server operations. Labels and free text never become tool names.
public static class AgentOutputValidator
{
    public static void Maintenance(MaintenanceMechanicOutput? output, Guid reportId)
    {
        if (output == null || output.BreakdownReportId != reportId.ToString() || string.IsNullOrWhiteSpace(output.LikelyPart)
            || output.EstimatedRepairHours is < 0 or > 8760 || output.Severity is not ("Low" or "Medium" or "High" or "Critical")
            || output.RecommendedAction is not ("ScheduleRepair" or "NeedsManualReview"))
            throw new InvalidOperationException("Maintenance output violates the diagnosis contract.");
    }
    public static void Plan(MissionPlannerOutput? plan, Guid requestId)
    {
        if (plan == null || plan.DispatchRequestId != requestId.ToString() || plan.Plan == null || plan.Plan.Count != 7)
            throw new InvalidOperationException("Planner output does not match the dispatch contract.");
        for (var i = 0; i < 7; i++)
        {
            var step = plan.Plan[i];
            if (step == null || step.StepNumber != i + 1 || string.IsNullOrWhiteSpace(step.StepName) || step.StepName.Length > 256
                || step.Status != "Pending" || step.AssignedAgent != (i < 5 ? "DispatchTelemetryAgent" : "SafetyGuardAgent"))
                throw new InvalidOperationException("Planner output contains an unsupported step or delegation.");
        }
    }

    public static void Telemetry(DispatchTelemetryAgentOutput? output, Guid requestId)
    {
        if (output == null || output.DispatchRequestId != requestId.ToString()
            || output.WeatherRisk is not ("low" or "medium" or "high" or "unknown")
            || output.Locked && (string.IsNullOrWhiteSpace(output.SelectedRoverId) || !output.BatteryOk || output.WeatherRisk is not ("low" or "medium")))
            throw new InvalidOperationException("Telemetry output violates the reservation contract.");
    }

    public static void Safety(SafetyGuardOutput? output, Guid requestId, string priority)
    {
        if (output == null || output.DispatchRequestId != requestId.ToString() || output.RiskScore is < 0 or > 100
            || string.IsNullOrWhiteSpace(output.RiskReason)
            || output.AutoOutcome is not ("AutoApproved" or "PendingApproval" or "AutoRejected")
            || output.RequiresApproval != (output.AutoOutcome == "PendingApproval")
            || priority == "Critical" && output.AutoOutcome == "AutoApproved")
            throw new InvalidOperationException("Safety output violates the authorization contract.");
    }
}
