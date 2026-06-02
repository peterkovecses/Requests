using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using System.Linq.Expressions;

namespace Kovecses.Requests;

/// <summary>
/// Provides extension methods for registering request handlers and behaviors.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all request handlers from the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for handlers.</param>
    /// <returns>A builder for configuring behaviors.</returns>
    public static IRequestsBuilder AddRequests(this IServiceCollection services, params Assembly[] assemblies)
    {
        var handlerType = typeof(IRequestHandler<,>);

        var handlers = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .SelectMany(type => type.GetInterfaces()
                .Where(interfaceType => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == handlerType)
                .Select(interfaceType => new { ServiceType = interfaceType, ImplementationType = type }));

        foreach (var handler in handlers)
        {
            services.TryAddTransient(handler.ImplementationType);

            var args = handler.ServiceType.GetGenericArguments();
            var pipelineHandlerType = typeof(PipelineRequestHandler<,>).MakeGenericType(args);
            var requestType = args[0];
            var responseType = args[1];
            var behaviorEnumerableType = typeof(IEnumerable<>).MakeGenericType(
                typeof(IPipelineBehavior<,>).MakeGenericType(requestType, responseType));

            var factory = BuildPipelineHandlerFactory(pipelineHandlerType, handler.ServiceType, behaviorEnumerableType);

            services.AddTransient(handler.ServiceType, sp =>
            {
                var inner = sp.GetRequiredService(handler.ImplementationType);
                var behaviors = sp.GetRequiredService(behaviorEnumerableType);
                
                return factory(inner, behaviors);
            });
        }

        return new RequestsBuilder(services, assemblies);
    }

    /// <summary>
    /// Registers all request handlers from the assemblies containing the specified marker types.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="handlerAssemblyMarkerTypes">The marker types used to identify the assemblies.</param>
    /// <returns>A builder for configuring behaviors.</returns>
    public static IRequestsBuilder AddRequests(this IServiceCollection services, params Type[] handlerAssemblyMarkerTypes)
        => services.AddRequests(handlerAssemblyMarkerTypes.Select(t => t.Assembly).ToArray());

    /// <summary>
    /// Registers all request handlers from the assembly containing the specified marker type.
    /// </summary>
    /// <typeparam name="TMarker">The marker type used to identify the assembly.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>A builder for configuring behaviors.</returns>
    public static IRequestsBuilder AddRequests<TMarker>(this IServiceCollection services)
        => services.AddRequests(typeof(TMarker).Assembly);

    private static Func<object, object, object> BuildPipelineHandlerFactory(
        Type pipelineHandlerType,
        Type handlerServiceType,
        Type behaviorEnumerableType)
    {
        var innerParam = Expression.Parameter(typeof(object), "inner");
        var behaviorsParam = Expression.Parameter(typeof(object), "behaviors");

        var inner = Expression.Convert(innerParam, handlerServiceType);
        var behaviors = Expression.Convert(behaviorsParam, behaviorEnumerableType);

        var constructor = pipelineHandlerType.GetConstructor([handlerServiceType, behaviorEnumerableType]);
        var newInstance = Expression.New(constructor!, inner, behaviors);

        var lambda = Expression.Lambda<Func<object, object, object>>(
            Expression.Convert(newInstance, typeof(object)),
            innerParam,
            behaviorsParam);

        return lambda.Compile();
    }
}