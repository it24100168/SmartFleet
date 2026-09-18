namespace SmartFleet.Backend.Services.Interfaces;

public class WeatherAssessmentResult
{
    /// <summary>
    /// Contract value: "low", "medium", or "high" exactly.
    /// </summary>
    public string WeatherRisk { get; set; } = "low";

    public string ConditionDescription { get; set; } = "Clear";

    public double TemperatureCelsius { get; set; } = 22.0;

    public double RainVolumeMm { get; set; } = 0.0;

    public bool IsSimulatedFallback { get; set; }
}

public interface IWeatherService
{
    Task<WeatherAssessmentResult> GetWeatherRiskAsync(
        string sourceZone,
        string destinationZone,
        CancellationToken cancellationToken = default);
}
