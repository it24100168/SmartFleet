using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartFleet.Backend.Agents.DispatchTelemetryAgent;
using SmartFleet.Backend.Agents.MaintenanceMechanicAgent;
using SmartFleet.Backend.Agents.MissionPlannerAgent;
using SmartFleet.Backend.Agents.SafetyGuardAgent;
using SmartFleet.Backend.Data;
using SmartFleet.Backend.Data.Repositories;
using SmartFleet.Backend.Middleware;
using SmartFleet.Backend.Services;
using SmartFleet.Backend.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true).AddEnvironmentVariables();
var demo = builder.Configuration.GetValue<bool>("Simulation:Enabled");
if (demo && !builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
    throw new InvalidOperationException("Simulation mode requires Development or Testing.");
var secret = builder.Configuration["JwtSettings:Secret"];
if (string.IsNullOrWhiteSpace(secret) && demo)
    builder.Configuration["JwtSettings:Secret"] = secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < 32)
    throw new InvalidOperationException("Configure JwtSettings__Secret with a private value of at least 32 bytes.");
builder.Services.AddDbContext<SmartFleetDbContext>(options =>
{
    if (demo) options.UseSqlite(builder.Configuration["Simulation:ConnectionString"] ?? "Data Source=smartfleet-demo.db;Default Timeout=15");
    else
    {
        var connection = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("Configure ConnectionStrings__DefaultConnection for PostgreSQL, or use the demo launcher.");
        options.UseNpgsql(connection);
    }
});
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoverRepository, RoverRepository>();
builder.Services.AddScoped<IDispatchRequestRepository, DispatchRequestRepository>();
builder.Services.AddScoped<IWorkflowRunRepository, WorkflowRunRepository>();
builder.Services.AddScoped<IBreakdownReportRepository, BreakdownReportRepository>();
builder.Services.AddScoped<IFailureCatalogRepository, FailureCatalogRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDispatchService, DispatchService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();

builder.Services.AddScoped<IMissionPlannerAgent, MissionPlannerAgent>();
builder.Services.AddScoped<IDispatchTelemetryAgent, DispatchTelemetryAgent>();
builder.Services.AddScoped<IMaintenanceMechanicAgent, MaintenanceMechanicAgent>();
builder.Services.AddScoped<ISafetyGuardAgent, SafetyGuardAgent>();
builder.Services.AddHttpClient<IWeatherService, WeatherService>();
builder.Services.AddScoped<WorkflowOrchestrator>();
builder.Services.AddHostedService<MissionSimulator>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "SmartFleetAPI",
        ValidateAudience = true, ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "SmartFleetClients",
        ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(10)
    });
builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:5173" })
    .AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "SmartFleet", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>() });
});
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads", "breakdowns"));
builder.Environment.WebRootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
builder.Environment.WebRootFileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(builder.Environment.WebRootPath);
var app = builder.Build();
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SmartFleetDbContext>();
    if (demo) { await db.Database.EnsureCreatedAsync(); await DemoSeeder.SeedAsync(db, builder.Configuration); }
    else if (builder.Configuration.GetValue<bool>("Database:ApplyMigrations")) await db.Database.MigrateAsync();
}
app.UseMiddleware<ExceptionHandlingMiddleware>();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseCors();
Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads", "breakdowns"));
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/api/health", async (SmartFleetDbContext db) => await db.Database.CanConnectAsync() ? Results.Ok(new { status = "ready", demo }) : Results.StatusCode(503));
app.Run();
public partial class Program { }
