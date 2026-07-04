namespace Meridian.Api.Infrastructure.Email;

/// <summary>
/// Abstraction for outbound email (password resets, etc.). The default
/// implementation logs messages; a real SMTP/provider implementation can be
/// dropped in without changing callers (Dependency Inversion).
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default);
}

/// <summary>Development-friendly sender that writes emails to the application log.</summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger) => _logger = logger;

    public Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation("EMAIL to {To} | {Subject}\n{Body}", toEmail, subject, body);
        return Task.CompletedTask;
    }
}
