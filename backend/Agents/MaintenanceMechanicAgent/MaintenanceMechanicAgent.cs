using SmartFleet.Backend.Agents.MaintenanceMechanicAgent.DTOs;
using SmartFleet.Backend.Data.Repositories;

namespace SmartFleet.Backend.Agents.MaintenanceMechanicAgent;

/// <summary>
/// Maintenance Mechanic Agent implementation coordinating automated failure diagnostics
/// using the allow-listed FailureCatalog knowledge base.
/// </summary>
public class MaintenanceMechanicAgent : IMaintenanceMechanicAgent
{
    private readonly IFailureCatalogRepository _failureCatalogRepository;
    private readonly ILogger<MaintenanceMechanicAgent> _logger;

    public MaintenanceMechanicAgent(
        IFailureCatalogRepository failureCatalogRepository,
        ILogger<MaintenanceMechanicAgent> logger)
    {
        _failureCatalogRepository = failureCatalogRepository;
        _logger = logger;
    }

    public async Task<MaintenanceMechanicOutput> DiagnoseAsync(MaintenanceMechanicInput input, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MaintenanceMechanicAgent initiating diagnosis for report {ReportId}, category: {Category}, code: {ErrorCode}",
            input.BreakdownReportId, input.SymptomCategory, input.ErrorCode);

        // Allow-listed knowledge tool query
        var match = await _failureCatalogRepository.FindMatchAsync(
            input.SymptomCategory,
            input.Description,
            input.ErrorCode,
            cancellationToken);

        if (match != null)
        {
            _logger.LogInformation("MaintenanceMechanicAgent matched catalog entry: {Keyword} -> {LikelyPart}",
                match.SymptomKeyword, match.LikelyPart);

            return new MaintenanceMechanicOutput
            {
                BreakdownReportId = input.BreakdownReportId,
                LikelyPart = match.LikelyPart,
                EstimatedRepairHours = match.EstimatedRepairHours,
                Severity = match.Severity,
                ConfidenceNote = $"Matched via FailureCatalog keyword: {match.SymptomKeyword}",
                RecommendedAction = "ScheduleRepair"
            };
        }

        // Deterministic fallback if no failure catalog pattern matches
        _logger.LogWarning("MaintenanceMechanicAgent found no catalog match for report {ReportId}. Returning NeedsManualReview.",
            input.BreakdownReportId);

        return new MaintenanceMechanicOutput
        {
            BreakdownReportId = input.BreakdownReportId,
            LikelyPart = "Unknown / Requires Inspection",
            EstimatedRepairHours = 0,
            Severity = "Medium",
            ConfidenceNote = "No automated failure pattern matched in FailureCatalog.",
            RecommendedAction = "NeedsManualReview"
        };
    }
}
