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
}
