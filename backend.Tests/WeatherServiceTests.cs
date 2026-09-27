using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SmartFleet.Backend.Services;
using Xunit;

namespace SmartFleet.Backend.Tests;

public class WeatherServiceTests
{
    private sealed class Handler(Func<int, HttpResponseMessage> response) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(response(++Calls));
    }
    private static WeatherService Service(Handler handler) => new(new HttpClient(handler),
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["WeatherSettings:ApiKey"]="test-secret" }).Build(),
        NullLogger<WeatherService>.Instance);
    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK) { Content=new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

    [Theory]
    [InlineData("{\"weather\":[],\"main\":{\"temp\":25}}")]
    [InlineData("not-json")]
    [InlineData("{}")]
    public async Task MalformedObservationsNeverBecomeClearWeather(string body)
    {
        var handler=new Handler(_=>Json(body));
        var result=await Service(handler).GetWeatherRiskAsync("A","B");
        Assert.Equal("unknown",result.WeatherRisk); Assert.Equal(2,handler.Calls);
        Assert.All(result.Attempts,a=> { Assert.Equal("InvalidResponse",a.Outcome); Assert.True(a.DurationMs>=0); });
    }
    [Fact]
    public async Task TransientErrorRetriesAndRecordsBothAttempts()
    {
        var handler=new Handler(n=>n==1 ? new(HttpStatusCode.ServiceUnavailable) : Json("{\"weather\":[{\"id\":800,\"description\":\"clear\"}],\"main\":{\"temp\":25}}"));
        var result=await Service(handler).GetWeatherRiskAsync("A","B");
        Assert.Equal("low",result.WeatherRisk); Assert.Equal(2,result.Attempts.Count);
        Assert.Equal("HttpError",result.Attempts[0].Outcome); Assert.Equal("Succeeded",result.Attempts[1].Outcome);
    }
    [Fact]
    public async Task AuthenticationFailureDoesNotRetry()
    {
        var handler=new Handler(_=>new(HttpStatusCode.Unauthorized));
        Assert.Equal("unknown",(await Service(handler).GetWeatherRiskAsync("A","B")).WeatherRisk);
        Assert.Equal(1,handler.Calls);
    }
    [Fact]
    public async Task CallerCancellationIsNotConvertedIntoWeather()
    {
        var handler=new Handler(_=>Json("{}")); using var cancel=new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>Service(handler).GetWeatherRiskAsync("A","B",cancel.Token));
        Assert.Equal(0,handler.Calls);
    }
}
