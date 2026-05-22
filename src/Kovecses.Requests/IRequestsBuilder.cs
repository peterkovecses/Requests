using Microsoft.Extensions.DependencyInjection;

namespace Kovecses.Requests;

/// <summary>
/// Defines a builder for configuring request handlers and behaviors.
/// </summary>
public interface IRequestsBuilder
{
    /// <summary>
    /// Gets the service collection.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Adds a global pipeline behavior that applies to all requests.
    /// </summary>
    /// <param name="openBehaviorType">The open generic type of the behavior (e.g., typeof(LoggingBehavior{,})).</param>
    /// <returns>The builder for chaining.</returns>
    IRequestsBuilder AddGlobalBehavior(Type openBehaviorType);

    /// <summary>
    /// Adds a pipeline behavior to all requests that implement the specified interface.
    /// </summary>
    /// <typeparam name="TInterface">The interface that requests must implement.</typeparam>
    /// <param name="openBehaviorType">The open generic type of the behavior.</param>
    /// <returns>The builder for chaining.</returns>
    IRequestsBuilder AddBehavior<TInterface>(Type openBehaviorType);

    /// <summary>
    /// Explicitly adds a pipeline behavior to a specific request and response type.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <typeparam name="TBehavior">The type of the behavior.</typeparam>
    /// <returns>The builder for chaining.</returns>
    IRequestsBuilder AddBehavior<TRequest, TResponse, TBehavior>()
        where TRequest : IRequest<TResponse>
        where TBehavior : class, IPipelineBehavior<TRequest, TResponse>;
}
