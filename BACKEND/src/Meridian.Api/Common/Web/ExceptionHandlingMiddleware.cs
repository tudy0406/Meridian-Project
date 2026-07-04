using System.Text.Json;
using Meridian.Api.Common.Exceptions;

namespace Meridian.Api.Common.Web;

/// <summary>
/// Translates exceptions into safe, consistent JSON problem responses. Expected
/// <see cref="AppException"/>s map to their status code; anything else becomes a
/// generic 500 with no implementation detail leaked (stack traces stay in logs).
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await WriteAsync(context, ex.StatusCode, ex.ErrorCode, ex.Message, ex.Errors);
        }
        catch (AppException ex)
        {
            // Expected business failures: log at information level, return the message.
            _logger.LogInformation("Handled {Error}: {Message}", ex.ErrorCode, ex.Message);
            await WriteAsync(context, ex.StatusCode, ex.ErrorCode, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception processing {Path}", context.Request.Path);
            var message = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            await WriteAsync(context, StatusCodes.Status500InternalServerError, "ServerError", message);
        }
    }

    private static Task WriteAsync(HttpContext context, int status, string code, string message,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        var payload = JsonSerializer.Serialize(new
        {
            error = code,
            message,
            errors
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return context.Response.WriteAsync(payload);
    }
}
