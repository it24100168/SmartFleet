namespace SmartFleet.Backend.Services;
public class MissionSimulator(IServiceScopeFactory scopes, IConfiguration config, ILogger<MissionSimulator> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (config.GetValue<bool>("Simulation:DisableWorker")) return;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try {
                using (var scope = scopes.CreateScope()) await scope.ServiceProvider.GetRequiredService<WorkflowOrchestrator>().TickAsync(stoppingToken);
                using (var scope = scopes.CreateScope()) await scope.ServiceProvider.GetRequiredService<WorkflowOrchestrator>().SchedulePendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogWarning(ex, "Simulation tick rolled back; will retry from persisted state."); }
        }
    }
}
