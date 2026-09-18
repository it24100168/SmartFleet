using Microsoft.Extensions.Logging;
using Moq;
using SmartFleet.Backend.Agents.DispatchTelemetryAgent;
using SmartFleet.Backend.Agents.DispatchTelemetryAgent.Contracts;
using SmartFleet.Backend.Data.Repositories;
using SmartFleet.Backend.Models;
using SmartFleet.Backend.Models.Enums;
using SmartFleet.Backend.Services.Interfaces;
using Xunit;

namespace SmartFleet.Backend.Tests;

public class DispatchTelemetryAgentTests
{
    private readonly Mock<IRoverRepository> _mockRoverRepo;
    private readonly Mock<IWeatherService> _mockWeatherService;
    private readonly Mock<ILogger<DispatchTelemetryAgent>> _mockLogger;
    private readonly DispatchTelemetryAgent _agent;

    public DispatchTelemetryAgentTests()
    {
        _mockRoverRepo = new Mock<IRoverRepository>();
        _mockWeatherService = new Mock<IWeatherService>();
        _mockLogger = new Mock<ILogger<DispatchTelemetryAgent>>();

        _agent = new DispatchTelemetryAgent(
            _mockRoverRepo.Object,
            _mockWeatherService.Object,
            _mockLogger.Object);
    }

    private static DispatchTelemetryAgentInput CreateSampleInput() => new()
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

    [Fact]
    public async Task ExecuteAsync_HealthyRoverAndLowWeatherRisk_ReturnsLockedOutputMatchingContract()
    {
        // Arrange
        var input = CreateSampleInput();
        var roverId = Guid.NewGuid();
        var rover = new Rover
        {
            Id = roverId,
            Identifier = "RO-04",
            Status = RoverStatus.Idle,
            BatteryPercentage = 88,
            LocationZone = "WarehouseA-DockA1"
        };

        _mockRoverRepo
            .Setup(r => r.GetAvailableRoversInZoneAsync("WarehouseA-DockA1", 40, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rover> { rover });

        _mockWeatherService
            .Setup(w => w.GetWeatherRiskAsync("WarehouseA-DockA1", "WarehouseA-DockB3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeatherAssessmentResult
            {
                WeatherRisk = "low",
                ConditionDescription = "Clear",
                TemperatureCelsius = 22.0
            });

        _mockRoverRepo
            .Setup(r => r.LockRoverForMissionAsync(roverId, "d3f1-892a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Rover
            {
                Id = roverId,
                Identifier = "RO-04",
                Status = RoverStatus.Dispatched,
                BatteryPercentage = 88,
                CurrentMissionId = "d3f1-892a"
            });

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert: Match exact contract fields and values from docs/agent-contracts.md
        Assert.NotNull(result);
        Assert.Equal("d3f1-892a", result.DispatchRequestId);
        Assert.Equal("RO-04", result.SelectedRoverId);
        Assert.True(result.BatteryOk);
        Assert.Equal("low", result.WeatherRisk);
        Assert.True(result.Locked);
        Assert.Null(result.Reason);
    }

    [Fact]
    public async Task ExecuteAsync_BatteryBelowSafeThreshold_RejectsAndSetsReason()
    {
        // Arrange: Rover in zone has only 25% battery (< 40% threshold)
        var input = CreateSampleInput();
        var lowBatteryRover = new Rover
        {
            Id = Guid.NewGuid(),
            Identifier = "RO-06",
            Status = RoverStatus.Idle,
            BatteryPercentage = 25,
            LocationZone = "WarehouseA-DockA1"
        };

        // No rovers in zone with >= 40% battery
        _mockRoverRepo
            .Setup(r => r.GetAvailableRoversInZoneAsync("WarehouseA-DockA1", 40, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rover>());

        // Fallback search returns the low battery rover
        _mockRoverRepo
            .Setup(r => r.GetPagedAsync(RoverStatus.Idle, null, "battery", true, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Rover> { lowBatteryRover }, 1));

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert contract fields
        Assert.NotNull(result);
        Assert.Equal("d3f1-892a", result.DispatchRequestId);
        Assert.Equal("RO-06", result.SelectedRoverId);
        Assert.False(result.BatteryOk);
        Assert.False(result.Locked);
        Assert.NotNull(result.Reason);
        Assert.Contains("below safe operating threshold", result.Reason);
    }

    [Fact]
    public async Task ExecuteAsync_HighWeatherRisk_HaltsDispatchAndSetsReason()
    {
        // Arrange
        var input = CreateSampleInput();
        var rover = new Rover
        {
            Id = Guid.NewGuid(),
            Identifier = "RO-04",
            Status = RoverStatus.Idle,
            BatteryPercentage = 90,
            LocationZone = "WarehouseA-DockA1"
        };

        _mockRoverRepo
            .Setup(r => r.GetAvailableRoversInZoneAsync("WarehouseA-DockA1", 40, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rover> { rover });

        // Mock OpenWeather returning "high" risk (severe storm/rain)
        _mockWeatherService
            .Setup(w => w.GetWeatherRiskAsync("WarehouseA-DockA1", "WarehouseA-DockB3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeatherAssessmentResult
            {
                WeatherRisk = "high",
                ConditionDescription = "Heavy Thunderstorm",
                RainVolumeMm = 28.5
            });

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert contract fields
        Assert.NotNull(result);
        Assert.Equal("d3f1-892a", result.DispatchRequestId);
        Assert.Equal("RO-04", result.SelectedRoverId);
        Assert.True(result.BatteryOk);
        Assert.Equal("high", result.WeatherRisk);
        Assert.False(result.Locked);
        Assert.NotNull(result.Reason);
        Assert.Contains("High weather risk", result.Reason);
    }

    [Fact]
    public async Task ExecuteAsync_NoRoversAvailable_ReturnsUnassignedFailureOutput()
    {
        // Arrange
        var input = CreateSampleInput();

        _mockRoverRepo
            .Setup(r => r.GetAvailableRoversInZoneAsync("WarehouseA-DockA1", 40, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rover>());

        _mockRoverRepo
            .Setup(r => r.GetPagedAsync(RoverStatus.Idle, null, "battery", true, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Rover>(), 0));

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("d3f1-892a", result.DispatchRequestId);
        Assert.Null(result.SelectedRoverId);
        Assert.False(result.BatteryOk);
        Assert.False(result.Locked);
        Assert.NotNull(result.Reason);
    }
}
