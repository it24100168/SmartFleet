namespace SmartFleet.Backend.Models.Enums;

/// <summary>
/// Defines the lifecycle status of an approval request.
/// </summary>
public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected,
    RevisionRequested
}
