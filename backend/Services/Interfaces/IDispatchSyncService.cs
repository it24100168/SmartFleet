namespace SmartFleet.Backend.Services.Interfaces;

/// <summary>
/// Internal synchronization service to notify teammate's DispatchRequest component of approval state changes.
/// </summary>
public interface IDispatchSyncService
{
    /// <summary>
    /// Propagates an approval decision to the linked dispatch request.
    /// </summary>
    Task<bool> UpdateDispatchRequestStatusAsync(string dispatchRequestId, string status, string? notes = null, CancellationToken cancellationToken = default);
}
