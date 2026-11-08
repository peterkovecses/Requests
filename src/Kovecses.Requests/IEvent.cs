namespace Kovecses.Requests;

/// <summary>Marks an event published to its registered handlers.</summary>
public interface IEvent;

/// <summary>Handles an event asynchronously.</summary>
/// <typeparam name="TEvent">The event type.</typeparam>
public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    /// <summary>Handles an event using the supplied cancellation token.</summary>
    /// <param name="event">The event to handle.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the handling operation.</returns>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
}

/// <summary>Publishes an event to its registered handlers.</summary>
/// <typeparam name="TEvent">The event type.</typeparam>
public interface IEventPublisher<TEvent> where TEvent : IEvent
{
    /// <summary>Publishes an event and waits for its handlers to finish.</summary>
    /// <param name="event">The event to publish.</param>
    /// <param name="cancellationToken">The cancellation token passed to handlers.</param>
    /// <returns>A task that succeeds when at least one handler exists and all handlers finish successfully.</returns>
    /// <exception cref="NoEventSubscribersException">No handlers are registered for the event type.</exception>
    /// <exception cref="AggregateException">One or more handlers failed in parallel mode.</exception>
    Task PublishAsync(TEvent @event, CancellationToken cancellationToken);
}
