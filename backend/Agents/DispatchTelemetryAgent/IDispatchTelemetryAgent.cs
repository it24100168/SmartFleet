using SmartFleet.Backend.Agents.DispatchTelemetryAgent.Contracts;

namespace SmartFleet.Backend.Agents.DispatchTelemetryAgent;

public interface IDispatchTelemetryAgent
{
    Task<DispatchTelemetryAgentOutput> ExecuteAsync(
        DispatchTelemetryAgentInput input,
        CancellationToken cancellationToken = default);

    DispatchTelemetryAgentInput GetMockInput();
}
