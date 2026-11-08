namespace Kovecses.Requests.UnitTests;

public class EventDiscoveryTests
{
    [Fact]
    public void AddRequests_OverloadsAndRepeatedAssemblies_ShouldRegisterEachScannedEventPairOnce()
    {
        var services = new ServiceCollection();

        services.AddRequests(typeof(DiscoveryFirstHandler).Assembly);
        services.AddRequests(typeof(DiscoveryFirstHandler));
        services.AddRequests<DiscoveryFirstHandler>();

        Assert.Equal(3, services.Count(descriptor =>
            descriptor.ServiceType == typeof(IEventHandler<DiscoveredEvent>)));
        Assert.Equal(1, services.Count(descriptor =>
            descriptor.ServiceType == typeof(IEventHandler<DiscoveredSecondEvent>)));

        Assert.All(
            services.Where(descriptor =>
                descriptor.ServiceType == typeof(IEventHandler<DiscoveredEvent>)
                || descriptor.ServiceType == typeof(IEventHandler<DiscoveredSecondEvent>)),
            descriptor => Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime));
    }

    [Fact]
    public void AddRequests_ShouldRetainDistinctImplementationsAndMultiEventHandlers()
    {
        var services = new ServiceCollection();
        services.AddRequests<DiscoveryFirstHandler>();
        using var provider = services.BuildServiceProvider();

        var firstEventHandlers = provider.GetServices<IEventHandler<DiscoveredEvent>>().ToArray();
        var secondEventHandlers = provider.GetServices<IEventHandler<DiscoveredSecondEvent>>().ToArray();

        Assert.Equal(
            new[] { typeof(DiscoveryFirstHandler), typeof(DiscoverySecondHandler), typeof(DiscoveryMultiHandler) }
                .OrderBy(type => type.Name),
            firstEventHandlers.Select(handler => handler.GetType()).OrderBy(type => type.Name));
        Assert.IsType<DiscoveryMultiHandler>(Assert.Single(secondEventHandlers));
    }

    [Fact]
    public void AddRequests_ShouldRegisterScannedHandlersAsTransient()
    {
        var services = new ServiceCollection();
        services.AddRequests<DiscoveryFirstHandler>();
        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<IEventHandler<DiscoveredEvent>>();
        var second = provider.GetRequiredService<IEventHandler<DiscoveredEvent>>();

        Assert.NotSame(first, second);
    }

    [Fact]
    public async Task PublishAsync_ShouldSupportManualInstanceRegistrations()
    {
        var calls = 0;
        var handler = new DelegateDiscoveryHandler<ManualEvent>((_, _) =>
        {
            calls++;
            return Task.CompletedTask;
        });
        var factoryCalls = 0;
        var services = new ServiceCollection();
        services.AddRequests<DiscoveryFirstHandler>();
        services.AddSingleton<IEventHandler<ManualEvent>>(handler);
        services.AddTransient<IEventHandler<ManualEvent>>(_ =>
        {
            factoryCalls++;
            return new DelegateDiscoveryHandler<ManualEvent>((_, _) =>
            {
                calls++;
                return Task.CompletedTask;
            });
        });
        using var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<IEventPublisher<ManualEvent>>()
            .PublishAsync(new ManualEvent(), CancellationToken.None);

        Assert.Equal(2, calls);
        Assert.Equal(1, factoryCalls);
        Assert.Same(handler, provider.GetServices<IEventHandler<ManualEvent>>().First());
    }

    [Fact]
    public void AddRequests_ShouldRegisterPublishersAsTransient()
    {
        var services = new ServiceCollection();
        services.AddRequests<DiscoveryFirstHandler>();
        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<IEventPublisher<ManualEvent>>();
        var second = provider.GetRequiredService<IEventPublisher<ManualEvent>>();

        Assert.NotSame(first, second);
    }

    [Fact]
    public void ResolvePublisher_WhenScannedHandlerDependencyIsMissing_ShouldFailBeforePublish()
    {
        var services = new ServiceCollection();
        services.AddRequests<BrokenDependencyHandler>();
        using var provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(
            () => provider.GetRequiredService<IEventPublisher<BrokenDependencyEvent>>());
    }

    [Fact]
    public async Task PublishAsync_ShouldShareScopedDependenciesWithinScopeAndSeparateThemAcrossScopes()
    {
        var services = new ServiceCollection();
        services.AddScoped<SharedScopedProbe>();
        services.AddScoped<ScopedObservations>();
        services.AddRequests<ScopedDependencyFirstHandler>();
        using var provider = services.BuildServiceProvider();

        SharedScopedProbe firstScopeProbe;
        await using (var scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IEventPublisher<ScopedDependencyEvent>>()
                .PublishAsync(new ScopedDependencyEvent(), CancellationToken.None);

            var observed = scope.ServiceProvider.GetRequiredService<ScopedObservations>();
            Assert.Equal(2, observed.Probes.Count);
            Assert.Same(observed.Probes[0], observed.Probes[1]);
            firstScopeProbe = observed.Probes[0];
        }

        Assert.True(firstScopeProbe.IsDisposed);

        await using (var scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IEventPublisher<ScopedDependencyEvent>>()
                .PublishAsync(new ScopedDependencyEvent(), CancellationToken.None);

            var observed = scope.ServiceProvider.GetRequiredService<ScopedObservations>();
            Assert.Equal(2, observed.Probes.Count);
            Assert.Same(observed.Probes[0], observed.Probes[1]);
            Assert.NotSame(firstScopeProbe, observed.Probes[0]);
        }
    }

    [Fact]
    public async Task PublishAsync_ShouldKeepScopedDependenciesAliveUntilAwaitedWorkAndScopeEnd()
    {
        var services = new ServiceCollection();
        services.AddScoped<ScopedWaitProbe>();
        services.AddScoped<ScopedWaitGate>();
        services.AddRequests<ScopedWaitHandler>();
        using var provider = services.BuildServiceProvider();
        ScopedWaitProbe probe;
        await using (var scope = provider.CreateAsyncScope())
        {
            probe = scope.ServiceProvider.GetRequiredService<ScopedWaitProbe>();
            var gate = scope.ServiceProvider.GetRequiredService<ScopedWaitGate>();

            var publish = scope.ServiceProvider.GetRequiredService<IEventPublisher<ScopedWaitEvent>>()
                .PublishAsync(new ScopedWaitEvent(), CancellationToken.None);
            await gate.Entered.Task;

            Assert.False(probe.IsDisposed);
            Assert.False(publish.IsCompleted);

            gate.Release.SetResult();
            await publish;
            Assert.False(probe.IsDisposed);
        }

        Assert.True(probe.IsDisposed);
    }

    [Fact]
    public async Task PublishAsync_Parallel_ShouldShareOneScopedDependencyAcrossHandlers()
    {
        var services = new ServiceCollection();
        services.AddScoped<SharedScopedProbe>();
        services.AddScoped<ScopedObservations>();
        services.AddRequests<ScopedDependencyFirstHandler>()
            .UseEventPublishStrategy(EventPublishStrategy.Parallel);
        using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<IEventPublisher<ParallelScopedDependencyEvent>>()
            .PublishAsync(new ParallelScopedDependencyEvent(), CancellationToken.None);

        var observed = scope.ServiceProvider.GetRequiredService<ScopedObservations>();
        Assert.Equal(2, observed.Probes.Count);
        Assert.Same(observed.Probes[0], observed.Probes[1]);
    }

    private sealed class DelegateDiscoveryHandler<TEvent>(
        Func<TEvent, CancellationToken, Task> handle) : IEventHandler<TEvent>
        where TEvent : IEvent
    {
        public Task HandleAsync(TEvent @event, CancellationToken cancellationToken) =>
            handle(@event, cancellationToken);
    }
}

public sealed record DiscoveredEvent : IEvent;
public sealed record DiscoveredSecondEvent : IEvent;
public sealed record ManualEvent : IEvent;
public sealed record BrokenDependencyEvent : IEvent;
public sealed record ScopedDependencyEvent : IEvent;
public sealed record ParallelScopedDependencyEvent : IEvent;
public sealed record ScopedWaitEvent : IEvent;

public sealed class DiscoveryFirstHandler : IEventHandler<DiscoveredEvent>
{
    public Task HandleAsync(DiscoveredEvent @event, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class DiscoverySecondHandler : IEventHandler<DiscoveredEvent>
{
    public Task HandleAsync(DiscoveredEvent @event, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class DiscoveryMultiHandler :
    IEventHandler<DiscoveredEvent>,
    IEventHandler<DiscoveredSecondEvent>
{
    public Task HandleAsync(DiscoveredEvent @event, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task HandleAsync(DiscoveredSecondEvent @event, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class BrokenDependencyHandler(UnregisteredHandlerDependency dependency)
    : IEventHandler<BrokenDependencyEvent>
{
    public Task HandleAsync(BrokenDependencyEvent @event, CancellationToken cancellationToken)
    {
        GC.KeepAlive(dependency);
        return Task.CompletedTask;
    }
}

public sealed class UnregisteredHandlerDependency
{
}

public sealed class ScopedDependencyFirstHandler(
    SharedScopedProbe probe,
    ScopedObservations observations) : IEventHandler<ScopedDependencyEvent>
{
    public Task HandleAsync(ScopedDependencyEvent @event, CancellationToken cancellationToken)
    {
        observations.Probes.Add(probe);
        return Task.CompletedTask;
    }
}

public sealed class ScopedDependencySecondHandler(
    SharedScopedProbe probe,
    ScopedObservations observations) : IEventHandler<ScopedDependencyEvent>
{
    public Task HandleAsync(ScopedDependencyEvent @event, CancellationToken cancellationToken)
    {
        observations.Probes.Add(probe);
        return Task.CompletedTask;
    }
}

public sealed class ParallelScopedDependencyFirstHandler(
    SharedScopedProbe probe,
    ScopedObservations observations) : IEventHandler<ParallelScopedDependencyEvent>
{
    public Task HandleAsync(ParallelScopedDependencyEvent @event, CancellationToken cancellationToken)
    {
        observations.Probes.Add(probe);
        return Task.CompletedTask;
    }
}

public sealed class ParallelScopedDependencySecondHandler(
    SharedScopedProbe probe,
    ScopedObservations observations) : IEventHandler<ParallelScopedDependencyEvent>
{
    public Task HandleAsync(ParallelScopedDependencyEvent @event, CancellationToken cancellationToken)
    {
        observations.Probes.Add(probe);
        return Task.CompletedTask;
    }
}

public sealed class SharedScopedProbe : IDisposable
{
    public bool IsDisposed { get; private set; }
    public void Dispose() => IsDisposed = true;
}

public sealed class ScopedObservations
{
    public List<SharedScopedProbe> Probes { get; } = [];
}

public sealed class ScopedWaitGate
{
    public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

public sealed class ScopedWaitProbe : IDisposable
{
    public bool IsDisposed { get; private set; }
    public void Dispose() => IsDisposed = true;
}

public sealed class ScopedWaitHandler(ScopedWaitProbe probe, ScopedWaitGate gate) : IEventHandler<ScopedWaitEvent>
{
    public async Task HandleAsync(ScopedWaitEvent @event, CancellationToken cancellationToken)
    {
        gate.Entered.TrySetResult();
        await gate.Release.Task;
        Assert.False(probe.IsDisposed);
    }
}
