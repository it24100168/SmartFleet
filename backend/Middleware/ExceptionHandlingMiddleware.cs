using System.Net;
using System.Text.Json;
using SmartFleet.Backend.DTOs.Common;

namespace SmartFleet.Backend.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred during HTTP request: {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => (HttpStatusCode.Conflict, "Another action changed this mission. Refresh and retry."),
            Microsoft.Data.Sqlite.SqliteException sqlite when sqlite.SqliteErrorCode is 5 or 6 => (HttpStatusCode.Conflict, "Fleet state is busy. Refresh and retry."),
            Npgsql.PostgresException pg when pg.SqlState is "40001" or "23505" => (HttpStatusCode.Conflict, "A concurrent action already changed this record. Refresh and retry."),
            UnauthorizedAccessException unauthorizedEx => (HttpStatusCode.Unauthorized, unauthorizedEx.Message),
            InvalidOperationException invalidOpEx => (HttpStatusCode.BadRequest, invalidOpEx.Message),
            ArgumentException argEx => (HttpStatusCode.BadRequest, argEx.Message),
            KeyNotFoundException notFoundEx => (HttpStatusCode.NotFound, notFoundEx.Message),
            _ => (HttpStatusCode.InternalServerError, "An unexpected internal server error occurred.")
        };

        context.Response.StatusCode = (int)statusCode;

        var response = new ErrorResponse
        {
            StatusCode = (int)statusCode,
            Message = message,
            Details = _environment.IsDevelopment() ? exception.StackTrace : null,
            Timestamp = DateTime.UtcNow
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}
