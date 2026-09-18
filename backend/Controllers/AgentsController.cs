using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartFleet.Backend.Agents.DispatchTelemetryAgent;
using SmartFleet.Backend.Agents.DispatchTelemetryAgent.Contracts;
using SmartFleet.Backend.Services.Interfaces;

namespace SmartFleet.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AgentsController : ControllerBase
{
    private readonly IDispatchTelemetryAgent _dispatchAgent;
    private readonly IWeatherService _weatherService;

    public AgentsController(
        IDispatchTelemetryAgent dispatchAgent,
        IWeatherService weatherService)
    {
        _dispatchAgent = dispatchAgent;
        _weatherService = weatherService;
    }

    /// <summary>
    /// Executes the Dispatch & Telemetry Agent against the provided plan input.
    /// Input and output strictly follow docs/agent-contracts.md.
    /// </summary>
    [HttpPost("dispatch-telemetry/execute")]
    [ProducesResponseType(typeof(DispatchTelemetryAgentOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExecuteDispatchTelemetryAgent(
        [FromBody] DispatchTelemetryAgentInput input,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input.DispatchRequestId))
        {
            return BadRequest(new { message = "dispatchRequestId is required." });
        }

        var result = await _dispatchAgent.ExecuteAsync(input, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns the exact mock input defined in docs/agent-contracts.md for testing and integration.
    /// </summary>
    [HttpGet("dispatch-telemetry/mock-input")]
    [ProducesResponseType(typeof(DispatchTelemetryAgentInput), StatusCodes.Status200OK)]
    public IActionResult GetMockInput()
    {
        return Ok(_dispatchAgent.GetMockInput());
    }

    /// <summary>
    /// Retrieves the current weather risk assessment for warehouse zones.
    /// </summary>
    [HttpGet("dispatch-telemetry/weather")]
    [ProducesResponseType(typeof(WeatherAssessmentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentWeather(
        [FromQuery] string sourceZone = "WarehouseA-DockA1",
        [FromQuery] string destinationZone = "WarehouseA-DockB3",
        CancellationToken cancellationToken = default)
    {
        var assessment = await _weatherService.GetWeatherRiskAsync(sourceZone, destinationZone, cancellationToken);
        return Ok(assessment);
    }
}
