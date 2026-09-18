namespace SmartFleet.Backend.Models;

/// <summary>
/// Persists agent execution state, planned step sequence, and progress against a DispatchRequest.
/// </summary>
public class WorkflowRun
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key referencing the parent DispatchRequest.
    /// </summary>
    public Guid DispatchRequestId { get; set; }

    /// <summary>
    /// Navigation property to the parent DispatchRequest.
    /// </summary>
    public DispatchRequest? DispatchRequest { get; set; }

    /// <summary>
    /// Serialized JSON representing the planning objective and input parameters.
    /// </summary>
    public string ObjectiveJson { get; set; } = string.Empty;

    /// <summary>
    /// Serialized JSON representing the multi-step checklist according to agent-contracts.md.
    /// </summary>
    public string PlanJson { get; set; } = string.Empty;

    /// <summary>
    /// Current step index in the plan workflow (1-indexed).
    /// </summary>
    public int CurrentStep { get; set; } = 1;

    /// <summary>
    /// Workflow execution state (e.g. Generated, InProgress, Completed, Failed).
    /// </summary>
    public string Status { get; set; } = "Generated";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
