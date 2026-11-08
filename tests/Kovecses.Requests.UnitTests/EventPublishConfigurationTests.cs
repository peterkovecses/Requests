namespace Kovecses.Requests.UnitTests;

public class EventPublishConfigurationTests
{
    [Fact]
    public async Task UseEventPublishStrategy_ShouldApplyLatestGlobalAndPerTypeValuesAcrossRepeatedRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddRequests<EventPublishConfigurationTests>();

        Assert.Same(builder, builder.UseEventPublishStrategy<TypeConfiguredBeforeGlobalEvent>(
            EventPublishStrategy.Sequential));
        builder.UseEventPublishStrategy(EventPublishStrategy.Parallel);
        builder.UseEventPublishStrategy<TypeConfiguredAfterGlobalEvent>(EventPublishStrategy.Sequential);
        builder.UseEventPublishStrategy<TypeConfiguredAfterGlobalEvent>(EventPublishStrategy.Parallel);
        builder.UseEventPublishStrategy<TypeConfiguredAfterGlobalEvent>(EventPublishStrategy.Sequential);
        builder.UseEventPublishStrategy(EventPublishStrategy.Sequential);
        builder.UseEventPublishStrategy(EventPublishStrategy.Parallel);

        services.AddRequests<EventPublishConfigurationTests>();
        AddStrategyProbe<TypeConfiguredBeforeGlobalEvent>(services);
        AddStrategyProbe<TypeConfiguredAfterGlobalEvent>(services);
        AddStrategyProbe<GlobalStrategyEvent>(services);
        using var provider = services.BuildServiceProvider();

        // Act & Assert
        await AssertUsesStrategyAsync<TypeConfiguredBeforeGlobalEvent>(provider, parallel: false);
        await AssertUsesStrategyAsync<TypeConfiguredAfterGlobalEvent>(provider, parallel: false);
        await AssertUsesStrategyAsync<GlobalStrategyEvent>(provider, parallel: true);
    }

    [Fact]
    public async Task UseEventPublishStrategy_WhenValueIsInvalid_ShouldLeaveBothConfigurationLevelsUnchanged()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddRequests<EventPublishConfigurationTests>();
        builder.UseEventPublishStrategy(EventPublishStrategy.Parallel);
        builder.UseEventPublishStrategy<InvalidTypeStrategyEvent>(EventPublishStrategy.Sequential);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => builder.UseEventPublishStrategy((EventPublishStrategy)999));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => builder.UseEventPublishStrategy<InvalidTypeStrategyEvent>((EventPublishStrategy)(-1)));

        AddStrategyProbe<GlobalAfterInvalidConfigurationEvent>(services);
        AddStrategyProbe<InvalidTypeStrategyEvent>(services);
        using var provider = services.BuildServiceProvider();

        // Assert
        await AssertUsesStrategyAsync<GlobalAfterInvalidConfigurationEvent>(provider, parallel: true);
        await AssertUsesStrategyAsync<InvalidTypeStrategyEvent>(provider, parallel: false);
    }

    [Fact]
    public async Task AddRequests_WhenMessagingIsConfigured_ShouldPreserveRequestHandlerAndPipelineBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRequests<EventPublishConfigurationTests>()
            .UseEventPublishStrategy(EventPublishStrategy.Parallel)
            .AddGlobalBehavior(typeof(RegressionBehavior<,>));
        using var provider = services.BuildServiceProvider();

        // Act
        var response = await provider.GetRequiredService<IRequestHandler<RegressionRequest, string>>()
            .HandleAsync(new RegressionRequest(), CancellationToken.None);

        // Assert
        Assert.Equal("Response with behavior", response);
    }

    private static void AddStrategyProbe<TEvent>(IServiceCollection services)
        where TEvent : IEvent
    {
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        services.AddSingleton<IEventHandler<TEvent>>(
            new DelegateEventHandler<TEvent>((_, _) =>
            {
                firstEntered.TrySetResult();

                return releaseFirst.Task;
            }));
        services.AddSingleton<IEventHandler<TEvent>>(
            new DelegateEventHandler<TEvent>((_, _) =>
            {
                secondEntered.TrySetResult();

                return Task.CompletedTask;
            }));
        StrategyProbes<TEvent>.Current = new StrategyProbe(firstEntered, releaseFirst, secondEntered);
    }

    private static async Task AssertUsesStrategyAsync<TEvent>(IServiceProvider provider, bool parallel)
        where TEvent : IEvent, new()
    {
        var probe = StrategyProbes<TEvent>.Current;
        var publish = provider.GetRequiredService<IEventPublisher<TEvent>>()
            .PublishAsync(new TEvent(), CancellationToken.None);

        await probe.FirstEntered.Task;
        if (parallel)
        {
            await probe.SecondEntered.Task;
            Assert.False(publish.IsCompleted);
            probe.ReleaseFirst.SetResult();
        }
        else
        {
            Assert.False(probe.SecondEntered.Task.IsCompleted);
            probe.ReleaseFirst.SetResult();
            await probe.SecondEntered.Task;
        }

        await publish;
    }

    private sealed class DelegateEventHandler<TEvent>(
        Func<TEvent, CancellationToken, Task> handle) : IEventHandler<TEvent>
        where TEvent : IEvent
    {
        public Task HandleAsync(TEvent @event, CancellationToken cancellationToken) =>
            handle(@event, cancellationToken);
    }

    private sealed record StrategyProbe(
        TaskCompletionSource FirstEntered,
        TaskCompletionSource ReleaseFirst,
        TaskCompletionSource SecondEntered);

    private static class StrategyProbes<TEvent>
    {
        public static StrategyProbe Current { get; set; } = null!;
    }

    private sealed record TypeConfiguredBeforeGlobalEvent : IEvent;
    private sealed record TypeConfiguredAfterGlobalEvent : IEvent;
    private sealed record GlobalStrategyEvent : IEvent;
    private sealed record InvalidTypeStrategyEvent : IEvent;
    private sealed record GlobalAfterInvalidConfigurationEvent : IEvent;
}

public sealed record RegressionRequest : IRequest<string>;

public sealed class RegressionRequestHandler : IRequestHandler<RegressionRequest, string>
{
    public Task<string> HandleAsync(RegressionRequest request, CancellationToken cancellationToken) =>
        Task.FromResult("Response");
}

public sealed class RegressionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        return response is string text
            ? (TResponse)(object)$"{text} with behavior"
            : response;
    }
}
