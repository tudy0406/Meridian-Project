namespace Meridian.Api.Common.Domain;

/// <summary>
/// Cross-cutting record of security-relevant events (logins, account/role
/// changes, structural modifications). Written by an Observer of the domain
/// event pipeline rather than by feature services directly.
/// </summary>
public class AuditLog : BaseEntity
{
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
