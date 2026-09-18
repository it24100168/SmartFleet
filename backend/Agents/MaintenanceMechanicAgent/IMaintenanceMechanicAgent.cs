using SmartFleet.Backend.Agents.MaintenanceMechanicAgent.DTOs;

namespace SmartFleet.Backend.Agents.MaintenanceMechanicAgent;

/// <summary>
/// Autonomous agent interface responsible for diagnosing rover mechanical failures
/// using structured symptom keywords and the FailureCatalog tool.
/// </summary>
public interface IMaintenanceMechanicAgent
{
    Task<MaintenanceMechanicOutput> DiagnoseAsync(MaintenanceMechanicInput input, CancellationToken cancellationToken = default);
}
