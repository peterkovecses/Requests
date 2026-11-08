using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kovecses.Requests;

/// <summary>
/// Provides extension methods for the <see cref="IRequestsBuilder"/>.
/// </summary>
public static class RequestsBuilderExtensions
{
    /// <summary>
    /// Adds OpenTelemetry tracing to the request pipeline.
    /// This adds an <see cref="Behaviors.OpenTelemetryBehavior{TRequest, TResponse}"/> as a global behavior.
    /// </summary>
    /// <param name="builder">The requests builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IRequestsBuilder AddOpenTelemetry(this IRequestsBuilder builder)
    {
        return builder.AddGlobalBehavior(typeof(Behaviors.OpenTelemetryBehavior<,>));
    }

    /// <summary>Sets the default strategy for publishing events.</summary>
    /// <param name="builder">The requests builder.</param>
    /// <param name="strategy">The strategy to use when no event-specific override exists.</param>
    /// <returns>The builder for chaining.</returns>
    public static IRequestsBuilder UseEventPublishStrategy(this IRequestsBuilder builder, EventPublishStrategy strategy)
        => SetEventPublishStrategy(builder, strategy, null);

    /// <summary>Overrides the publishing strategy for an event type.</summary>
    /// <typeparam name="TEvent">The event type to configure.</typeparam>
    /// <param name="builder">The requests builder.</param>
    /// <param name="strategy">The strategy for this event type.</param>
    /// <returns>The builder for chaining.</returns>
    public static IRequestsBuilder UseEventPublishStrategy<TEvent>(this IRequestsBuilder builder, EventPublishStrategy strategy)
        where TEvent : IEvent
        => SetEventPublishStrategy(builder, strategy, typeof(TEvent));

    private static IRequestsBuilder SetEventPublishStrategy(
        IRequestsBuilder builder, EventPublishStrategy strategy, Type? eventType)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (strategy is not EventPublishStrategy.Sequential and not EventPublishStrategy.Parallel)
        {
            throw new ArgumentOutOfRangeException(nameof(strategy));
        }

        var descriptor = builder.Services.LastOrDefault(d => d.ServiceType == typeof(EventPublishConfiguration));
        var configuration = descriptor?.ImplementationInstance as EventPublishConfiguration ?? EventPublishConfiguration.Default;
        var updated = eventType is null
            ? configuration.WithGlobalStrategy(strategy)
            : configuration.WithEventStrategy(eventType, strategy);
        builder.Services.Replace(ServiceDescriptor.Singleton(updated));
        return builder;
    }
}
