namespace Kovecses.Requests.UnitTests;

public class EventPublisherTests
{
    [Fact]
    public async Task PublishAsync_WhenNoHandlersExist_ShouldThrowWithTheEventType()
    {
        // Arrange
        await using var provider = CreateProvider();
        var publisher = provider.GetRequiredService<IEventPublisher<UnsubscribedEvent>>();

        // Act
        var exception = await Assert.ThrowsAsync<NoEventSubscribersException>(
            () => publisher.PublishAsync(new UnsubscribedEvent(), CancellationToken.None));

        // Assert
        Assert.Equal(typeof(UnsubscribedEvent), exception.EventType);
    }

    [Fact]
    public async Task PublishAsync_WhenEventIsNull_ShouldThrowBeforeCheckingCancellationOrSubscribers()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await using var provider = CreateProvider();
        var publisher = provider.GetRequiredService<IEventPublisher<UnsubscribedEvent>>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => publisher.PublishAsync(null!, cancellation.Token));
    }

    [Fact]
    public async Task PublishAsync_WhenAlreadyCanceledAndNoHandlersExist_ShouldThrowCancellationFirst()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await using var provider = CreateProvider();
        var publisher = provider.GetRequiredService<IEventPublisher<UnsubscribedEvent>>();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => publisher.PublishAsync(new UnsubscribedEvent(), cancellation.Token));
    }

    [Fact]
    public async Task PublishAsync_WhenAlreadyCanceled_ShouldNotInvokeRegisteredHandlers()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var calls = 0;
        await using var provider = CreateProvider(services =>
            services.AddSingleton<IEventHandler<PreCanceledEvent>>(
                new DelegateEventHandler<PreCanceledEvent>((_, _) =>
                {
                    calls++;

                    return Task.CompletedTask;
                })));

        // Act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => provider.GetRequiredService<IEventPublisher<PreCanceledEvent>>()
                .PublishAsync(new PreCanceledEvent(), cancellation.Token));

        // Assert
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task PublishAsync_WhenSequential_ShouldPreserveOrderEventAndToken()
    {
        // Arrange
        var received = new List<string>();
        var message = new SequentialEvent();
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        await using var provider = CreateProvider(services =>
        {
            services.AddSingleton<IEventHandler<SequentialEvent>>(
                new DelegateEventHandler<SequentialEvent>((actual, actualToken) =>
                {
                    Assert.Same(message, actual);
                    Assert.Equal(token, actualToken);
                    received.Add("first");

                    return Task.CompletedTask;
                }));
            services.AddSingleton<IEventHandler<SequentialEvent>>(
                new DelegateEventHandler<SequentialEvent>((actual, actualToken) =>
                {
                    Assert.Same(message, actual);
                    Assert.Equal(token, actualToken);
                    received.Add("second");

                    return Task.CompletedTask;
                }));
        });

        // Act
        await provider.GetRequiredService<IEventPublisher<SequentialEvent>>()
            .PublishAsync(message, token);

        // Assert
        Assert.Equal(["first", "second"], received);
    }

    [Fact]
    public async Task PublishAsync_WhenSequential_ShouldWaitForEachHandlerBeforeStartingTheNext()
    {
        // Arrange
        var firstEntered = NewCompletionSource();
        var releaseFirst = NewCompletionSource();
        var secondEntered = NewCompletionSource();
        var releaseSecond = NewCompletionSource();
        await using var provider = CreateProvider(services =>
        {
            services.AddSingleton<IEventHandler<SequentialWaitEvent>>(
                new DelegateEventHandler<SequentialWaitEvent>(async (_, _) =>
                {
                    firstEntered.TrySetResult();
                    await releaseFirst.Task;
                }));
            services.AddSingleton<IEventHandler<SequentialWaitEvent>>(
                new DelegateEventHandler<SequentialWaitEvent>(async (_, _) =>
                {
                    secondEntered.TrySetResult();
                    await releaseSecond.Task;
                }));
        });

        // Act & Assert
        var publish = provider.GetRequiredService<IEventPublisher<SequentialWaitEvent>>()
            .PublishAsync(new SequentialWaitEvent(), CancellationToken.None);

        await firstEntered.Task;
        Assert.False(publish.IsCompleted);
        Assert.False(secondEntered.Task.IsCompleted);

        releaseFirst.SetResult();
        await secondEntered.Task;
        Assert.False(publish.IsCompleted);
        releaseSecond.SetResult();
        await publish;
    }

    [Fact]
    public async Task PublishAsync_WhenSequentialAndHandlerFailsSynchronously_ShouldStop()
    {
        // Arrange
        var laterHandlerCalls = 0;
        var failure = new InvalidOperationException("sync failure");
        await using var provider = CreateProvider(services =>
        {
            services.AddSingleton<IEventHandler<SequentialFailureEvent>>(
                new DelegateEventHandler<SequentialFailureEvent>((_, _) => throw failure));
            services.AddSingleton<IEventHandler<SequentialFailureEvent>>(
                new DelegateEventHandler<SequentialFailureEvent>((_, _) =>
                {
                    laterHandlerCalls++;

                    return Task.CompletedTask;
                }));
        });

        // Act
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetRequiredService<IEventPublisher<SequentialFailureEvent>>()
                .PublishAsync(new SequentialFailureEvent(), CancellationToken.None));

        // Assert
        Assert.Same(failure, actual);
        Assert.Equal(0, laterHandlerCalls);
    }

    [Fact]
    public async Task PublishAsync_WhenSequentialAndHandlerFailsAsynchronously_ShouldStop()
    {
        // Arrange
        var laterHandlerCalls = 0;
        var failure = new InvalidOperationException("async failure");
        await using var provider = CreateProvider(services =>
        {
            services.AddSingleton<IEventHandler<SequentialAsyncFailureEvent>>(
                new DelegateEventHandler<SequentialAsyncFailureEvent>(
                    (_, _) => Task.FromException(failure)));
            services.AddSingleton<IEventHandler<SequentialAsyncFailureEvent>>(
                new DelegateEventHandler<SequentialAsyncFailureEvent>((_, _) =>
                {
                    laterHandlerCalls++;

                    return Task.CompletedTask;
                }));
        });

        // Act
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetRequiredService<IEventPublisher<SequentialAsyncFailureEvent>>()
                .PublishAsync(new SequentialAsyncFailureEvent(), CancellationToken.None));

        // Assert
        Assert.Same(failure, actual);
        Assert.Equal(0, laterHandlerCalls);
    }

    [Fact]
    public async Task PublishAsync_WhenSequentialAndHandlerCancelsToken_ShouldStop()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var laterHandlerCalls = 0;
        await using var provider = CreateProvider(services =>
        {
            services.AddSingleton<IEventHandler<SequentialCancellationEvent>>(
                new DelegateEventHandler<SequentialCancellationEvent>((_, _) =>
                {
                    cancellation.Cancel();

                    return Task.CompletedTask;
                }));
            services.AddSingleton<IEventHandler<SequentialCancellationEvent>>(
                new DelegateEventHandler<SequentialCancellationEvent>((_, _) =>
                {
                    laterHandlerCalls++;

                    return Task.CompletedTask;
                }));
        });

        // Act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => provider.GetRequiredService<IEventPublisher<SequentialCancellationEvent>>()
                .PublishAsync(new SequentialCancellationEvent(), cancellation.Token));

        // Assert
        Assert.Equal(0, laterHandlerCalls);
    }

    [Fact]
    public async Task PublishAsync_WhenSequentialAndHandlerCancels_ShouldPropagateCancellationAndStop()
    {
        // Arrange
        var cancellationFailure = new OperationCanceledException("handler canceled");
        var laterHandlerCalls = 0;
        await using var provider = CreateProvider(services =>
        {
            services.AddSingleton<IEventHandler<SequentialHandlerCancellationEvent>>(
                new DelegateEventHandler<SequentialHandlerCancellationEvent>(
                    (_, _) => Task.FromException(cancellationFailure)));
            services.AddSingleton<IEventHandler<SequentialHandlerCancellationEvent>>(
                new DelegateEventHandler<SequentialHandlerCancellationEvent>((_, _) =>
                {
                    laterHandlerCalls++;

                    return Task.CompletedTask;
                }));
        });

        // Act
        var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => provider.GetRequiredService<IEventPublisher<SequentialHandlerCancellationEvent>>()
                .PublishAsync(new SequentialHandlerCancellationEvent(), CancellationToken.None));

        // Assert
        Assert.Same(cancellationFailure, actual);
        Assert.Equal(0, laterHandlerCalls);
    }

    [Fact]
    public async Task PublishAsync_WhenHandlerReturnsNullTask_ShouldThrowInvalidOperationException()
    {
        // Arrange
        await using var provider = CreateProvider(services =>
            services.AddSingleton<IEventHandler<NullTaskEvent>>(
                new DelegateEventHandler<NullTaskEvent>((_, _) => null!)));

        // Act
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetRequiredService<IEventPublisher<NullTaskEvent>>()
                .PublishAsync(new NullTaskEvent(), CancellationToken.None));

        // Assert
        Assert.Contains("null task", actual.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PublishAsync_WhenParallel_ShouldStartEveryHandlerAndWaitForEveryCompletion()
    {
        // Arrange
        var firstEntered = NewCompletionSource();
        var secondEntered = NewCompletionSource();
        var releaseFirst = NewCompletionSource();
        var releaseSecond = NewCompletionSource();
        await using var provider = CreateProvider(services =>
        {
            services.AddRequests<EventPublisherTests>()
                .UseEventPublishStrategy(EventPublishStrategy.Parallel);
            services.AddSingleton<IEventHandler<ParallelWaitEvent>>(
                new DelegateEventHandler<ParallelWaitEvent>(async (_, _) =>
                {
                    firstEntered.TrySetResult();
                    await releaseFirst.Task;
                }));
            services.AddSingleton<IEventHandler<ParallelWaitEvent>>(
                new DelegateEventHandler<ParallelWaitEvent>(async (_, _) =>
                {
                    Assert.True(firstEntered.Task.IsCompleted);
                    secondEntered.TrySetResult();
                    await releaseSecond.Task;
                }));
        });

        // Act & Assert
        var publish = provider.GetRequiredService<IEventPublisher<ParallelWaitEvent>>()
            .PublishAsync(new ParallelWaitEvent(), CancellationToken.None);

        await firstEntered.Task;
        await secondEntered.Task;
        Assert.False(publish.IsCompleted);

        releaseFirst.SetResult();
        Assert.False(publish.IsCompleted);
        releaseSecond.SetResult();
        await publish;
    }

    [Fact]
    public async Task PublishAsync_WhenParallelAndHandlerFailsSynchronously_ShouldStartOtherHandlers()
    {
        // Arrange
        var calls = new List<string>();
        var firstFailure = new InvalidOperationException("first");
        await using var provider = CreateProvider(services =>
        {
            services.AddRequests<EventPublisherTests>()
                .UseEventPublishStrategy(EventPublishStrategy.Parallel);
            services.AddSingleton<IEventHandler<ParallelSyncFailureEvent>>(
                new DelegateEventHandler<ParallelSyncFailureEvent>((_, _) =>
                {
                    calls.Add("first");
                    throw firstFailure;
                }));
            services.AddSingleton<IEventHandler<ParallelSyncFailureEvent>>(
                new DelegateEventHandler<ParallelSyncFailureEvent>((_, _) =>
                {
                    calls.Add("second");

                    return Task.CompletedTask;
                }));
        });

        // Act
        var actual = await Assert.ThrowsAsync<AggregateException>(
            () => provider.GetRequiredService<IEventPublisher<ParallelSyncFailureEvent>>()
                .PublishAsync(new ParallelSyncFailureEvent(), CancellationToken.None));

        // Assert
        Assert.Equal(["first", "second"], calls);
        Assert.Collection(actual.InnerExceptions, exception => Assert.Same(firstFailure, exception));
    }

    [Fact]
    public async Task PublishAsync_WhenParallel_ShouldAggregateAllFaultsAndCancellationsInHandlerOrder()
    {
        // Arrange
        var first = new InvalidOperationException("first handler");
        var taskFaultOne = new ArgumentException("task fault one");
        var taskFaultTwo = new ApplicationException("task fault two");
        var cancellationFailure = new OperationCanceledException("handler cancellation");
        var last = new NotSupportedException("last handler");
        await using var provider = CreateProvider(services =>
        {
            services.AddRequests<EventPublisherTests>()
                .UseEventPublishStrategy(EventPublishStrategy.Parallel);
            services.AddSingleton<IEventHandler<ParallelErrorsEvent>>(
                new DelegateEventHandler<ParallelErrorsEvent>((_, _) => throw first));
            services.AddSingleton<IEventHandler<ParallelErrorsEvent>>(
                new DelegateEventHandler<ParallelErrorsEvent>((_, _) =>
                    Task.WhenAll(Task.FromException(taskFaultOne), Task.FromException(taskFaultTwo))));
            services.AddSingleton<IEventHandler<ParallelErrorsEvent>>(
                new DelegateEventHandler<ParallelErrorsEvent>((_, _) =>
                    Task.FromException(cancellationFailure)));
            services.AddSingleton<IEventHandler<ParallelErrorsEvent>>(
                new DelegateEventHandler<ParallelErrorsEvent>((_, _) =>
                    Task.FromException(last)));
        });

        // Act
        var actual = await Assert.ThrowsAsync<AggregateException>(
            () => provider.GetRequiredService<IEventPublisher<ParallelErrorsEvent>>()
                .PublishAsync(new ParallelErrorsEvent(), CancellationToken.None));

        // Assert
        Assert.Collection(
            actual.InnerExceptions,
            exception => Assert.Same(first, exception),
            exception => Assert.Same(taskFaultOne, exception),
            exception => Assert.Same(taskFaultTwo, exception),
            exception => Assert.Same(cancellationFailure, exception),
            exception => Assert.Same(last, exception));
    }

    [Fact]
    public async Task PublishAsync_WhenParallelAndOnlyHandlersCancel_ShouldPropagateFirstCancellation()
    {
        // Arrange
        var first = new OperationCanceledException("first");
        var second = new OperationCanceledException("second");
        await using var provider = CreateProvider(services =>
        {
            services.AddRequests<EventPublisherTests>()
                .UseEventPublishStrategy(EventPublishStrategy.Parallel);
            services.AddSingleton<IEventHandler<ParallelCancellationEvent>>(
                new DelegateEventHandler<ParallelCancellationEvent>((_, _) => Task.FromException(first)));
            services.AddSingleton<IEventHandler<ParallelCancellationEvent>>(
                new DelegateEventHandler<ParallelCancellationEvent>((_, _) => Task.FromException(second)));
        });

        // Act
        var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => provider.GetRequiredService<IEventPublisher<ParallelCancellationEvent>>()
                .PublishAsync(new ParallelCancellationEvent(), CancellationToken.None));

        // Assert
        Assert.Same(first, actual);
    }

    [Fact]
    public async Task PublishAsync_WhenParallelAndTokenIsCanceledDuringInvocation_ShouldStillStartAllHandlers()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var received = new List<CancellationToken>();
        await using var provider = CreateProvider(services =>
        {
            services.AddRequests<EventPublisherTests>()
                .UseEventPublishStrategy(EventPublishStrategy.Parallel);
            services.AddSingleton<IEventHandler<ParallelStartCancellationEvent>>(
                new DelegateEventHandler<ParallelStartCancellationEvent>((_, token) =>
                {
                    received.Add(token);
                    cancellation.Cancel();

                    return Task.CompletedTask;
                }));
            services.AddSingleton<IEventHandler<ParallelStartCancellationEvent>>(
                new DelegateEventHandler<ParallelStartCancellationEvent>((_, token) =>
                {
                    received.Add(token);

                    return Task.CompletedTask;
                }));
        });

        // Act
        await provider.GetRequiredService<IEventPublisher<ParallelStartCancellationEvent>>()
            .PublishAsync(new ParallelStartCancellationEvent(), cancellation.Token);

        // Assert
        Assert.Equal(2, received.Count);
        Assert.All(received, token => Assert.Equal(cancellation.Token, token));
    }

    [Fact]
    public async Task PublishAsync_WhenParallelAndHandlersIgnoreCancellation_ShouldSucceedAfterAllComplete()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var firstEntered = NewCompletionSource();
        var releaseFirst = NewCompletionSource();
        var secondEntered = NewCompletionSource();
        var releaseSecond = NewCompletionSource();
        await using var provider = CreateProvider(services =>
        {
            services.AddRequests<EventPublisherTests>()
                .UseEventPublishStrategy(EventPublishStrategy.Parallel);
            services.AddSingleton<IEventHandler<ParallelIgnoredCancellationEvent>>(
                new DelegateEventHandler<ParallelIgnoredCancellationEvent>(async (_, _) =>
                {
                    firstEntered.TrySetResult();
                    await releaseFirst.Task;
                }));
            services.AddSingleton<IEventHandler<ParallelIgnoredCancellationEvent>>(
                new DelegateEventHandler<ParallelIgnoredCancellationEvent>(async (_, _) =>
                {
                    secondEntered.TrySetResult();
                    await releaseSecond.Task;
                }));
        });

        // Act & Assert
        var publish = provider.GetRequiredService<IEventPublisher<ParallelIgnoredCancellationEvent>>()
            .PublishAsync(new ParallelIgnoredCancellationEvent(), cancellation.Token);
        await firstEntered.Task;
        await secondEntered.Task;
        cancellation.Cancel();
        releaseFirst.SetResult();
        releaseSecond.SetResult();

        await publish;
    }

    [Fact]
    public async Task PublishAsync_WhenParallelAndHandlerReturnsNullTask_ShouldAggregateAndContinue()
    {
        // Arrange
        var secondHandlerCalled = false;
        await using var provider = CreateProvider(services =>
        {
            services.AddRequests<EventPublisherTests>()
                .UseEventPublishStrategy(EventPublishStrategy.Parallel);
            services.AddSingleton<IEventHandler<ParallelNullTaskEvent>>(
                new DelegateEventHandler<ParallelNullTaskEvent>((_, _) => null!));
            services.AddSingleton<IEventHandler<ParallelNullTaskEvent>>(
                new DelegateEventHandler<ParallelNullTaskEvent>((_, _) =>
                {
                    secondHandlerCalled = true;

                    return Task.CompletedTask;
                }));
        });

        // Act
        var actual = await Assert.ThrowsAsync<AggregateException>(
            () => provider.GetRequiredService<IEventPublisher<ParallelNullTaskEvent>>()
                .PublishAsync(new ParallelNullTaskEvent(), CancellationToken.None));

        // Assert
        Assert.True(secondHandlerCalled);
        Assert.IsType<InvalidOperationException>(Assert.Single(actual.InnerExceptions));
    }

    [Fact]
    public async Task PublishAsync_ShouldNotResolveHandlersForAnAssignableEventType()
    {
        // Arrange
        await using var provider = CreateProvider(services =>
            services.AddSingleton<IEventHandler<DerivedPolymorphicEvent>>(
                new DelegateEventHandler<DerivedPolymorphicEvent>((_, _) => Task.CompletedTask)));

        // Act & Assert
        await Assert.ThrowsAsync<NoEventSubscribersException>(
            () => provider.GetRequiredService<IEventPublisher<IEvent>>()
                .PublishAsync(new DerivedPolymorphicEvent(), CancellationToken.None));
    }

    private static ServiceProvider CreateProvider(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddRequests<EventPublisherTests>();
        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }

    private static TaskCompletionSource NewCompletionSource() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class DelegateEventHandler<TEvent>(
        Func<TEvent, CancellationToken, Task> handle) : IEventHandler<TEvent>
        where TEvent : IEvent
    {
        public Task HandleAsync(TEvent @event, CancellationToken cancellationToken) =>
            handle(@event, cancellationToken);
    }

    private sealed record UnsubscribedEvent : IEvent;
    private sealed record PreCanceledEvent : IEvent;
    private sealed record SequentialEvent : IEvent;
    private sealed record SequentialWaitEvent : IEvent;
    private sealed record SequentialFailureEvent : IEvent;
    private sealed record SequentialAsyncFailureEvent : IEvent;
    private sealed record SequentialCancellationEvent : IEvent;
    private sealed record SequentialHandlerCancellationEvent : IEvent;
    private sealed record NullTaskEvent : IEvent;
    private sealed record ParallelWaitEvent : IEvent;
    private sealed record ParallelSyncFailureEvent : IEvent;
    private sealed record ParallelErrorsEvent : IEvent;
    private sealed record ParallelCancellationEvent : IEvent;
    private sealed record ParallelStartCancellationEvent : IEvent;
    private sealed record ParallelIgnoredCancellationEvent : IEvent;
    private sealed record ParallelNullTaskEvent : IEvent;
    private sealed record DerivedPolymorphicEvent : PolymorphicBaseEvent;
    private abstract record PolymorphicBaseEvent : IEvent;
}
