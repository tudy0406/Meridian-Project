using Microsoft.Extensions.DependencyInjection;

namespace Meridian.Api.Common.Domain.Events;

/// <summary>
/// Default dispatcher that resolves all handlers for each event's concrete type
/// from the DI container and invokes them. Handler failures are isolated and
/// logged so that one misbehaving observer cannot break the others.
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handlers = _serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                if (handler is null) continue;
                try
                {
                    var task = (Task)handlerType
                        .GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!
                        .Invoke(handler, new object[] { domainEvent, cancellationToken })!;
                    await task;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Domain event handler {Handler} failed for event {Event}",
                        handler.GetType().Name, domainEvent.GetType().Name);
                }
            }
        }
    }
}
