namespace SmartFleet.Backend.Agents.MissionPlannerAgent;

/// <summary>
/// Service interface for the Mission Planner Agent.
/// Decomposes incoming dispatch requests into a planned multi-step checklist for downstream execution agents.
/// </summary>
public interface IMissionPlannerAgent
{
    /// <summary>
    /// Generates a structured multi-step mission plan labeled with assigned agents.
    /// </summary>
    Task<MissionPlannerOutput> GeneratePlanAsync(MissionPlannerInput input, CancellationToken cancellationToken = default);
}
