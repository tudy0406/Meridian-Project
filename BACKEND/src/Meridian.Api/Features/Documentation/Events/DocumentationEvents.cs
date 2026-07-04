using Meridian.Api.Common.Domain;
using Meridian.Api.Common.Domain.Events;
using Meridian.Api.Features.Notifications;
using Meridian.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Meridian.Api.Features.Documentation.Events;

/// <summary>Raised when a team's onboarding documentation is created or updated.</summary>
public sealed record DocumentationUpdatedEvent(
    int TeamId,
    string DocumentationTitle) : IDomainEvent;

/// <summary>
/// Observer that notifies the onboarding employees of a team when its
/// documentation changes (notifications are limited to onboarding employees).
/// </summary>
public sealed class DocumentationUpdatedNotificationHandler : IDomainEventHandler<DocumentationUpdatedEvent>
{
    private readonly INotifier _notifier;
    private readonly MeridianDbContext _db;

    public DocumentationUpdatedNotificationHandler(INotifier notifier, MeridianDbContext db)
    {
        _notifier = notifier;
        _db = db;
    }

    public async Task HandleAsync(DocumentationUpdatedEvent e, CancellationToken ct = default)
    {
        var recipients = await _db.Users
            .Where(u => u.TeamId == e.TeamId && u.IsOnboarding)
            .Select(u => u.Id)
            .ToListAsync(ct);

        if (recipients.Count == 0) return;

        await _notifier.NotifyManyAsync(recipients, NotificationType.DocumentationUpdated,
            "Documentation updated",
            $"Team documentation \"{e.DocumentationTitle}\" was updated.", ct);
    }
}
