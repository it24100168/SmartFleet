using SmartFleet.Backend.Agents.SafetyGuardAgent.DTOs;

namespace SmartFleet.Backend.Agents.SafetyGuardAgent;

/// <summary>
/// Safety Guard Agent contract interface.
/// </summary>
public interface ISafetyGuardAgent
{
    /// <summary>
    /// Evaluates the aggregated mission, telemetry, and maintenance data against deterministic safety rules.
    /// Sets autoOutcome to "AutoApproved", "PendingApproval", or "AutoRejected".
    /// If requiresApproval is true, creates a pending ApprovalRequest in the database (Human-in-the-Loop pause).
    /// </summary>
    Task<SafetyGuardOutput> EvaluateSafetyAsync(SafetyGuardInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates mocked input objects matching the contract shape for testing and demonstration.
    /// </summary>
    SafetyGuardInput CreateMockedInput(string scenario = "low-risk", string? dispatchRequestId = null, string? roverId = null);
}
