namespace Kovecses.Requests;

internal sealed class EventPublisher<TEvent>(
    IEnumerable<IEventHandler<TEvent>> handlers,
    EventPublishConfiguration configuration) : IEventPublisher<TEvent>
    where TEvent : IEvent
{
    private readonly IEventHandler<TEvent>[] _handlers = handlers.ToArray();
    private readonly EventPublishStrategy _strategy = configuration.GetStrategy(typeof(TEvent));

    public async Task PublishAsync(TEvent @event, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);
        cancellationToken.ThrowIfCancellationRequested();

        if (_handlers.Length == 0)
        {
            throw new NoEventSubscribersException(typeof(TEvent));
        }

        if (_strategy == EventPublishStrategy.Sequential)
        {
            foreach (var handler in _handlers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var task = handler.HandleAsync(@event, cancellationToken)
                    ?? throw new InvalidOperationException("An event handler returned a null task.");
                await task.ConfigureAwait(false);
            }

            return;
        }

        var invocations = new Task<List<Exception>>[_handlers.Length];
        for (var i = 0; i < _handlers.Length; i++)
        {
            invocations[i] = CaptureErrorsAsync(_handlers[i], @event, cancellationToken);
        }

        var results = await Task.WhenAll(invocations).ConfigureAwait(false);
        var errors = results.SelectMany(result => result).ToArray();
        if (errors.Any(error => error is not OperationCanceledException))
        {
            throw new AggregateException(errors);
        }

        if (errors.Length != 0)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(errors[0]).Throw();
        }
    }

    private static async Task<List<Exception>> CaptureErrorsAsync(
        IEventHandler<TEvent> handler, TEvent @event, CancellationToken cancellationToken)
    {
        Task? task = null;
        try
        {
            task = handler.HandleAsync(@event, cancellationToken)
                ?? throw new InvalidOperationException("An event handler returned a null task.");
            await task.ConfigureAwait(false);
            return [];
        }
        catch (Exception exception)
        {
            return task?.Exception is { } aggregate ? [.. aggregate.InnerExceptions] : [exception];
        }
    }
}
