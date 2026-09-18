using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartFleet.Backend.Agents.MaintenanceMechanicAgent;
using SmartFleet.Backend.Data;
using SmartFleet.Backend.Data.Repositories;
using SmartFleet.Backend.Middleware;
using SmartFleet.Backend.Services;
using SmartFleet.Backend.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------------
// 1. Database Configuration (EF Core + Npgsql)
// --------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["ConnectionStrings__DefaultConnection"]
    ?? "Host=localhost;Port=5432;Database=smartfleet;Username=postgres;Password=postgres";

// Convert postgresql:// URI format to standard ADO.NET format if supplied
if (!string.IsNullOrEmpty(connectionString) && (connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) || connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)))
{
    try
    {
        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':');
        var user = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
        var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var db = uri.AbsolutePath.TrimStart('/');
        connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={pass};SSL Mode=Require;Trust Server Certificate=true;";
    }
    catch
    {
        // Fall back to sanitizing channel_binding
        connectionString = connectionString.Replace("&channel_binding=require", "")
                                           .Replace("?channel_binding=require&", "?")
                                           .Replace("?channel_binding=require", "");
    }
}

builder.Services.AddDbContext<SmartFleetDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsAssembly(typeof(SmartFleetDbContext).Assembly.FullName);
        npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
    });
});

// --------------------------------------------------
// 2. Application Services & Repositories
// --------------------------------------------------
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IBreakdownReportRepository, BreakdownReportRepository>();
builder.Services.AddScoped<IFailureCatalogRepository, FailureCatalogRepository>();
builder.Services.AddScoped<IMaintenanceMechanicAgent, MaintenanceMechanicAgent>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// --------------------------------------------------
// 3. Authentication & JWT Configuration
// --------------------------------------------------
var jwtSecret = builder.Configuration["JwtSettings:Secret"]
    ?? builder.Configuration["JwtSettings__Secret"]
    ?? "SmartFleetSuperSecretKeyForJwtAuthentication2026!MustBeAtLeast32BytesLong";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"]
    ?? builder.Configuration["JwtSettings__Issuer"]
    ?? "SmartFleetAPI";
var jwtAudience = builder.Configuration["JwtSettings:Audience"]
    ?? builder.Configuration["JwtSettings__Audience"]
    ?? "SmartFleetClients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// --------------------------------------------------
// 4. CORS Policy (React & Flutter Origins)
// --------------------------------------------------
const string CorsPolicyName = "SmartFleetCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy.SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrEmpty(origin)) return false;
                var uri = new Uri(origin);
                // Allow localhost on any port for React/Flutter web development
                return uri.Host == "localhost" || uri.Host == "127.0.0.1";
            })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// --------------------------------------------------
// 5. Controllers & JSON Formatting
// --------------------------------------------------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// --------------------------------------------------
// 6. Swagger / OpenAPI with JWT Bearer Support
// --------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SmartFleet API",
        Version = "v1",
        Description = "Backend API for SmartFleet autonomous warehouse rover management system."
    });

    // Configure JWT Bearer Authorization in Swagger UI
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter 'Bearer' [space] followed by your valid JWT token.\nExample: Bearer eyJhbGciOi...",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

// --------------------------------------------------
// 7. HTTP Pipeline Configuration
// --------------------------------------------------
var app = builder.Build();

// Automatically apply EF Core migrations against PostgreSQL on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Checking and applying pending database migrations...");
        var db = services.GetRequiredService<SmartFleetDbContext>();
        db.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to apply database migrations automatically on startup.");
    }
}

// Global exception handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Enable Swagger in Development & Staging
if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartFleet API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors(CorsPolicyName);
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Export Program class for test fixtures if needed
public partial class Program { }
