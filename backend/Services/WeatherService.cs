using System.Net.Http.Json;
using System.Text.Json;
using SmartFleet.Backend.Services.Interfaces;

namespace SmartFleet.Backend.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WeatherService> _logger;

    public WeatherService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<WeatherService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromSeconds(6);
    }

    public async Task<WeatherAssessmentResult> GetWeatherRiskAsync(
        string sourceZone,
        string destinationZone,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["WeatherSettings:ApiKey"]
            ?? _configuration["OPENWEATHER_API_KEY"]
            ?? _configuration["WeatherSettings__ApiKey"];

        var city = _configuration["WeatherSettings:City"] ?? "Colombo";

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_OPENWEATHER_API_KEY")
        {
            _logger.LogInformation("OpenWeather API key is not configured. Utilizing simulated warehouse weather assessment.");
            return GenerateSimulatedWeather(sourceZone, destinationZone);
        }

        // Retry logic with timeout and error handling
        const int maxRetries = 2;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var requestUrl = $"https://api.openweathermap.org/data/2.5/weather?q={Uri.EscapeDataString(city)}&appid={apiKey}&units=metric";
                using var response = await _httpClient.GetAsync(requestUrl, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenWeather API returned status code {StatusCode}. Attempt {Attempt} of {MaxRetries}.", response.StatusCode, attempt, maxRetries);
                    if (attempt == maxRetries) break;
                    await Task.Delay(500 * attempt, cancellationToken);
                    continue;
                }

                var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
                return ParseOpenWeatherResponse(json);
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "Transient error fetching OpenWeather API on attempt {Attempt}. Retrying...", attempt);
                await Task.Delay(500 * attempt, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reach OpenWeather API after {MaxRetries} attempts. Falling back to simulated weather.", maxRetries);
            }
        }

        return GenerateSimulatedWeather(sourceZone, destinationZone);
    }

    private static WeatherAssessmentResult ParseOpenWeatherResponse(JsonElement json)
    {
        var weatherArray = json.GetProperty("weather");
        var conditionId = 800;
        var description = "Clear";

        if (weatherArray.GetArrayLength() > 0)
        {
            var firstWeather = weatherArray[0];
            conditionId = firstWeather.GetProperty("id").GetInt32();
            description = firstWeather.GetProperty("description").GetString() ?? "Clear";
        }

        var temp = json.GetProperty("main").GetProperty("temp").GetDouble();
        var rainVolume = 0.0;
        if (json.TryGetProperty("rain", out var rainElement) && rainElement.TryGetProperty("1h", out var rain1h))
        {
            rainVolume = rain1h.GetDouble();
        }

        // Map condition code to contract weatherRisk: "low", "medium", "high"
        var risk = conditionId switch
        {
            // Thunderstorm
            >= 200 and < 300 => "high",
            // Heavy/Extreme Rain
            502 or 503 or 504 or 511 or 522 or 531 => "high",
            // Light to Moderate Rain / Drizzle
            (>= 300 and < 400) or 500 or 501 or 520 or 521 => "medium",
            // Snow / Sleet
            >= 600 and < 700 => "medium",
            // Fog / Mist / Atmosphere
            >= 700 and < 800 => "medium",
            // Clear / Clouds
            _ => "low"
        };

        return new WeatherAssessmentResult
        {
            WeatherRisk = risk,
            ConditionDescription = description,
            TemperatureCelsius = temp,
            RainVolumeMm = rainVolume,
            IsSimulatedFallback = false
        };
    }

    private static WeatherAssessmentResult GenerateSimulatedWeather(string sourceZone, string destinationZone)
    {
        // Safe deterministic simulated weather for warehouse indoor/outdoor dock logistics
        return new WeatherAssessmentResult
        {
            WeatherRisk = "low",
            ConditionDescription = "Indoor Warehouse Protected / Clear",
            TemperatureCelsius = 22.5,
            RainVolumeMm = 0.0,
            IsSimulatedFallback = true
        };
    }
}
