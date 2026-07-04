using Meridian.Api.Common.Domain;
using Meridian.Api.Infrastructure.Persistence;

namespace Meridian.Api.Infrastructure.Audit;

/// <summary>
/// Records security-relevant events (logins, account/role/structural changes).
/// Kept as an explicit service so events that are not tied to an entity change
/// — such as a failed login — are still reliably audited.
/// </summary>
public interface IAuditLogger
{
    Task LogAsync(string action, string entityName, string? entityId = null, int? userId = null, CancellationToken ct = default);
}

public sealed class AuditLogger : IAuditLogger
{
    private readonly MeridianDbContext _db;

    public AuditLogger(MeridianDbContext db) => _db = db;

    public async Task LogAsync(string action, string entityName, string? entityId = null, int? userId = null, CancellationToken ct = default)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            UserId = userId,
            Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }
}
