using SmartFleet.Backend.Services.Interfaces;

namespace SmartFleet.Backend.Services;

/// <summary>
/// Internal synchronization service updating DispatchRequest status.
/// Operates cleanly even while DispatchRequest entity lives in a teammate's branch.
/// </summary>
public class DispatchSyncService : IDispatchSyncService
{
    private readonly ILogger<DispatchSyncService> _logger;

    public DispatchSyncService(ILogger<DispatchSyncService> logger)
    {
        _logger = logger;
    }

    public Task<bool> UpdateDispatchRequestStatusAsync(string dispatchRequestId, string status, string? notes = null, CancellationToken cancellationToken = default)
    {
        // Internal service call: In full integration, this updates the DispatchRequest entity
        // or publishes an event/message queue notification.
        _logger.LogInformation(
            "[DispatchSyncService] DispatchRequest {DispatchRequestId} transitioned to status '{Status}'. Notes: {Notes}",
            dispatchRequestId,
            status,
            notes ?? "No notes provided"
        );

        return Task.FromResult(true);
    }
}
