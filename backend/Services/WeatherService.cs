using System.Net.Http.Json;
using System.Diagnostics;
using System.Text.Json;
using System.Globalization;
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
        var provider = _configuration["WeatherSettings:Provider"]
            ?? (string.IsNullOrWhiteSpace(apiKey) ? "OpenMeteo" : "OpenWeather");
        var city = _configuration["WeatherSettings:City"] ?? "Colombo";
        if (provider is not ("OpenMeteo" or "OpenWeather"))
        {
            _logger.LogWarning("Weather provider is not supported.");
            return UnavailableWeather();
        }
        if (provider == "OpenWeather" && (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_OPENWEATHER_API_KEY"))
        {
            _logger.LogInformation("OpenWeather API key is not configured. Dispatch weather is unavailable.");
            return UnavailableWeather();
        }

        var requestUrl = provider == "OpenWeather"
            ? $"https://api.openweathermap.org/data/2.5/weather?q={Uri.EscapeDataString(city)}&appid={Uri.EscapeDataString(apiKey!)}&units=metric"
            : OpenMeteoUrl();
        if (requestUrl == null) return UnavailableWeather();

        // Retry logic with timeout and error handling
        const int maxRetries = 2;
        var attempts = new List<WeatherAttempt>();
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var timer = Stopwatch.StartNew();
            try
            {
                using var response = await _httpClient.GetAsync(requestUrl, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    attempts.Add(new(attempt, timer.ElapsedMilliseconds, "HttpError", (int)response.StatusCode));
                    _logger.LogWarning("OpenWeather API returned status code {StatusCode}. Attempt {Attempt} of {MaxRetries}.", response.StatusCode, attempt, maxRetries);
                    if (attempt == maxRetries || (int)response.StatusCode is >= 400 and < 500 && (int)response.StatusCode != 429) break;
                    await Task.Delay(500 * attempt, cancellationToken);
                    continue;
                }

                var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
                var result = provider == "OpenWeather" ? ParseOpenWeatherResponse(json) : ParseOpenMeteoResponse(json);
                attempts.Add(new(attempt, timer.ElapsedMilliseconds, "Succeeded", (int)response.StatusCode));
                result.Attempts = attempts;
                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
            {
                // Do not store exception messages or URLs: provider URLs contain the API key.
                attempts.Add(new(attempt, timer.ElapsedMilliseconds, ex is OperationCanceledException ? "Timeout" : ex is HttpRequestException ? "NetworkError" : "InvalidResponse"));
                _logger.LogWarning("Weather assessment attempt {Attempt} failed ({Outcome}).", attempt, attempts[^1].Outcome);
                if (attempt < maxRetries) await Task.Delay(500 * attempt, cancellationToken);
            }
        }

        var unavailable = UnavailableWeather();
        unavailable.Attempts = attempts;
        return unavailable;
    }

    private string? OpenMeteoUrl()
    {
        var latitudeText = _configuration["WeatherSettings:Latitude"] ?? "6.9271";
        var longitudeText = _configuration["WeatherSettings:Longitude"] ?? "79.8612";
        if (!double.TryParse(latitudeText, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude)
            || !double.TryParse(longitudeText, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude)
            || !double.IsFinite(latitude) || !double.IsFinite(longitude)
            || latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            _logger.LogWarning("Open-Meteo coordinates are invalid.");
            return null;
        }
        return $"https://api.open-meteo.com/v1/forecast?latitude={latitude.ToString(CultureInfo.InvariantCulture)}&longitude={longitude.ToString(CultureInfo.InvariantCulture)}&current=temperature_2m,precipitation,weather_code&timezone=UTC";
    }

    private static WeatherAssessmentResult ParseOpenMeteoResponse(JsonElement json)
    {
        var current = json.GetProperty("current");
        var code = current.GetProperty("weather_code").GetInt32();
        var temperature = current.GetProperty("temperature_2m").GetDouble();
        var precipitation = current.GetProperty("precipitation").GetDouble();
        if (!double.IsFinite(temperature) || temperature is < -100 or > 70
            || !double.IsFinite(precipitation) || precipitation < 0)
            throw new JsonException("Weather values are outside expected ranges.");
        var risk = code switch
        {
            >= 0 and <= 3 => "low",
            45 or 48 or 51 or 53 or 55 or 56 or 57 or 61 or 63 or 71 or 73 or 75 or 77 or 80 or 81 or 85 or 86 => "medium",
            65 or 66 or 67 or 82 or 95 or 96 or 99 => "high",
            _ => "unknown"
        };
        return new WeatherAssessmentResult
        {
            WeatherRisk = risk,
            ConditionDescription = $"Open-Meteo WMO code {code}",
            TemperatureCelsius = temperature,
            RainVolumeMm = precipitation,
            IsSimulatedFallback = false
        };
    }

    private static WeatherAssessmentResult ParseOpenWeatherResponse(JsonElement json)
    {
        var weatherArray = json.GetProperty("weather");
        if (weatherArray.GetArrayLength() == 0) throw new JsonException("Missing weather observation.");
        var conditionId = 0;
        var description = "Unknown";

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
            >= 800 and <= 804 => "low",
            _ => "unknown"
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

    private static WeatherAssessmentResult UnavailableWeather()
    {
        // Safe deterministic simulated weather for warehouse indoor/outdoor dock logistics
        return new WeatherAssessmentResult
        {
            WeatherRisk = "unknown",
            ConditionDescription = "Weather unavailable. Dispatch blocked until a valid assessment is available.",
            TemperatureCelsius = 22.5,
            RainVolumeMm = 0.0,
            IsSimulatedFallback = false
        };
    }
}
