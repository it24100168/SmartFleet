namespace SmartFleet.Backend.Agents.MissionPlannerAgent;

/// <summary>
/// Mission Planner Agent (owner: Chathumini).
/// 
/// ARCHITECTURAL BOUNDARY:
/// MissionPlannerAgent is strictly a planner and orchestrator. It converts incoming dispatch
/// requests into a structured multi-step task checklist and labels each step with its assignedAgent.
/// 
/// NOTE: This agent does NOT contain any logic that checks battery levels, calls weather APIs,
/// queries rover telemetry, or performs diagnostics. Those execution tasks are delegated exclusively
/// to downstream agents (DispatchTelemetryAgent, MaintenanceMechanicAgent, SafetyGuardAgent).
/// </summary>
public class MissionPlannerAgent : IMissionPlannerAgent
{
    private readonly ILogger<MissionPlannerAgent> _logger;

    public MissionPlannerAgent(ILogger<MissionPlannerAgent> logger)
    {
        _logger = logger;
    }

    public Task<MissionPlannerOutput> GeneratePlanAsync(MissionPlannerInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        _logger.LogInformation(
            "MissionPlannerAgent: Generating plan for DispatchRequestId={DispatchRequestId}, Cargo={CargoType}, Priority={Priority}",
            input.DispatchRequestId,
            input.CargoType,
            input.Priority);

        var steps = new List<MissionPlanStep>
        {
            new()
            {
                StepNumber = 1,
                StepName = "Locate available rover",
                Status = "Pending",
                AssignedAgent = "DispatchTelemetryAgent"
            },
            new()
            {
                StepNumber = 2,
                StepName = "Verify battery sufficient for distance",
                Status = "Pending",
                AssignedAgent = "DispatchTelemetryAgent"
            },
            new()
            {
                StepNumber = 3,
                StepName = "Check weather risk",
                Status = "Pending",
                AssignedAgent = "DispatchTelemetryAgent"
            },
            new()
            {
                StepNumber = 4,
                StepName = "Reserve rover",
                Status = "Pending",
                AssignedAgent = "DispatchTelemetryAgent"
            },
            new()
            {
                StepNumber = 5,
                StepName = string.IsNullOrWhiteSpace(input.SourceZone) || string.IsNullOrWhiteSpace(input.DestinationZone)
                    ? "Schedule route"
                    : $"Schedule route from {input.SourceZone} to {input.DestinationZone}",
                Status = "Pending",
                AssignedAgent = "DispatchTelemetryAgent"
            },
            new()
            {
                StepNumber = 6,
                StepName = "Evaluate mission safety bounds",
                Status = "Pending",
                AssignedAgent = "SafetyGuardAgent"
            },
            new()
            {
                StepNumber = 7,
                StepName = "Await approval",
                Status = "Pending",
                AssignedAgent = "SafetyGuardAgent"
            }
        };

        var output = new MissionPlannerOutput
        {
            DispatchRequestId = input.DispatchRequestId,
            Plan = steps,
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        return Task.FromResult(output);
    }
}
