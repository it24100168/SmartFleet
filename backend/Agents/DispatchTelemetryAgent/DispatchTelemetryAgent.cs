using SmartFleet.Backend.Agents.DispatchTelemetryAgent.Contracts;
using SmartFleet.Backend.Data.Repositories;
using SmartFleet.Backend.Models;
using SmartFleet.Backend.Models.Enums;
using SmartFleet.Backend.Services.Interfaces;

namespace SmartFleet.Backend.Agents.DispatchTelemetryAgent;

public class DispatchTelemetryAgent : IDispatchTelemetryAgent
{
    private readonly IRoverRepository _roverRepository;
    private readonly IWeatherService _weatherService;
    private readonly ILogger<DispatchTelemetryAgent> _logger;

    // Minimum safe battery percentage required to initiate a dispatch mission
    private const int SafeBatteryThreshold = 40;

    public DispatchTelemetryAgent(
        IRoverRepository roverRepository,
        IWeatherService weatherService,
        ILogger<DispatchTelemetryAgent> logger)
    {
        _roverRepository = roverRepository;
        _weatherService = weatherService;
        _logger = logger;
    }

    public async Task<DispatchTelemetryAgentOutput> ExecuteAsync(
        DispatchTelemetryAgentInput input,
        CancellationToken cancellationToken = default)
    {
        // -------------------------------------------------------------------------------------------------
        // TODO: Connect real hand-off from Mission Planner Agent (Chathumini) after integration week.
        // During development and isolated testing, this agent executes against mocked or API-provided input
        // conforming to the contract locked in docs/agent-contracts.md.
        // -------------------------------------------------------------------------------------------------

        _logger.LogInformation("Dispatch & Telemetry Agent executing for DispatchRequestId: {RequestId}, Source: {Source}, Dest: {Dest}",
            input.DispatchRequestId, input.SourceZone, input.DestinationZone);

        // 1. Query available rovers matching zone and battery threshold against real Rover table
        var candidatesInZone = await _roverRepository.GetAvailableRoversInZoneAsync(
            input.SourceZone,
            minBattery: SafeBatteryThreshold,
            cancellationToken);

        Rover? selectedRover = candidatesInZone.FirstOrDefault();

        // If no candidate in exact source zone with >=40% battery, inspect any idle rover
        if (selectedRover == null)
        {
            var (allIdleRovers, _) = await _roverRepository.GetPagedAsync(
                status: RoverStatus.Idle,
                zone: null,
                sortBy: "battery",
                isDescending: true,
                page: 1,
                pageSize: 10,
                cancellationToken);

            var fallbackCandidate = allIdleRovers.FirstOrDefault();

            if (fallbackCandidate == null)
            {
                _logger.LogWarning("Dispatch rejected: No idle rovers available in system for {RequestId}.", input.DispatchRequestId);
                return new DispatchTelemetryAgentOutput
                {
                    DispatchRequestId = input.DispatchRequestId,
                    SelectedRoverId = null,
                    BatteryOk = false,
                    WeatherRisk = "low",
                    Locked = false,
                    Reason = $"No available idle rovers found in zone '{input.SourceZone}' or warehouse fleet."
                };
            }

            // Check if available rover has insufficient battery
            if (fallbackCandidate.BatteryPercentage < SafeBatteryThreshold)
            {
                _logger.LogWarning("Dispatch rejected: Candidate rover {RoverId} battery ({Battery}%) below safe threshold {Threshold}%.",
                    fallbackCandidate.Identifier, fallbackCandidate.BatteryPercentage, SafeBatteryThreshold);

                return new DispatchTelemetryAgentOutput
                {
                    DispatchRequestId = input.DispatchRequestId,
                    SelectedRoverId = fallbackCandidate.Identifier,
                    BatteryOk = false,
                    WeatherRisk = "low",
                    Locked = false,
                    Reason = $"Selected rover '{fallbackCandidate.Identifier}' battery ({fallbackCandidate.BatteryPercentage}%) is below safe operating threshold ({SafeBatteryThreshold}%)."
                };
            }

            selectedRover = fallbackCandidate;
        }

        // 2. Query OpenWeather API for environmental weather risk along route
        var weatherResult = await _weatherService.GetWeatherRiskAsync(
            input.SourceZone,
            input.DestinationZone,
            cancellationToken);

        _logger.LogInformation("Weather risk evaluated for route: {Risk} ({Desc})",
            weatherResult.WeatherRisk, weatherResult.ConditionDescription);

        // Deterministic validation: reject if weather risk is "high"
        if (weatherResult.WeatherRisk == "high")
        {
            _logger.LogWarning("Dispatch halted: High weather risk detected along transit route for {RequestId}.", input.DispatchRequestId);
            return new DispatchTelemetryAgentOutput
            {
                DispatchRequestId = input.DispatchRequestId,
                SelectedRoverId = selectedRover.Identifier,
                BatteryOk = true,
                WeatherRisk = "high",
                Locked = false,
                Reason = $"High weather risk detected along transit route ({weatherResult.ConditionDescription}); dispatch halted for safety."
            };
        }

        // 3. Transactionally lock the selected rover for the mission to prevent race conditions
        try
        {
            var lockedRover = await _roverRepository.LockRoverForMissionAsync(
                selectedRover.Id,
                input.DispatchRequestId,
                cancellationToken);

            if (lockedRover == null)
            {
                return new DispatchTelemetryAgentOutput
                {
                    DispatchRequestId = input.DispatchRequestId,
                    SelectedRoverId = selectedRover.Identifier,
                    BatteryOk = true,
                    WeatherRisk = weatherResult.WeatherRisk,
                    Locked = false,
                    Reason = $"Rover '{selectedRover.Identifier}' was concurrently locked by another process."
                };
            }

            _logger.LogInformation("Rover {RoverId} successfully locked for mission {RequestId}.",
                lockedRover.Identifier, input.DispatchRequestId);

            // Return exact output contract
            return new DispatchTelemetryAgentOutput
            {
                DispatchRequestId = input.DispatchRequestId,
                SelectedRoverId = lockedRover.Identifier,
                BatteryOk = true,
                WeatherRisk = weatherResult.WeatherRisk, // "low" or "medium"
                Locked = true,
                Reason = null
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Concurrency lock failure for rover {RoverId}: {Message}", selectedRover.Identifier, ex.Message);
            return new DispatchTelemetryAgentOutput
            {
                DispatchRequestId = input.DispatchRequestId,
                SelectedRoverId = selectedRover.Identifier,
                BatteryOk = true,
                WeatherRisk = weatherResult.WeatherRisk,
                Locked = false,
                Reason = ex.Message
            };
        }
    }

    /// <summary>
    /// Returns the exact mock input matching docs/agent-contracts.md for testing.
    /// </summary>
    public DispatchTelemetryAgentInput GetMockInput()
    {
        return new DispatchTelemetryAgentInput
        {
            DispatchRequestId = "d3f1-892a",
            PlanSteps = new List<PlanStepDto>
            {
                new() { StepNumber = 1, StepName = "Locate available rover", Status = "Pending" },
                new() { StepNumber = 2, StepName = "Verify battery sufficient", Status = "Pending" }
            },
            SourceZone = "WarehouseA-DockA1",
            DestinationZone = "WarehouseA-DockB3"
        };
    }
}
