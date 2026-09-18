namespace SmartFleet.Backend.Models.Enums;

/// <summary>
/// Defines the lifecycle status of a dispatch request order.
/// </summary>
public enum DispatchRequestStatus
{
    Pending,
    Planned,
    AwaitingApproval,
    Approved,
    Rejected,
    InTransit,
    Completed,
    Failed
}
