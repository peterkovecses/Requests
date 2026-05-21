using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Kovecses.Requests;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRequests(this IServiceCollection services, params Assembly[] assemblies)
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

            services.AddTransient(handler.ServiceType, sp =>
            {
                var inner = sp.GetRequiredService(handler.ImplementationType);

                return ActivatorUtilities.CreateInstance(sp, pipelineHandlerType, inner);
            });
        }

        return services;
    }

    public static IServiceCollection AddRequests(this IServiceCollection services, params Type[] handlerAssemblyMarkerTypes)
        => services.AddRequests(handlerAssemblyMarkerTypes.Select(t => t.Assembly).ToArray());

    public static IServiceCollection AddRequests<TMarker>(this IServiceCollection services)
        => services.AddRequests(typeof(TMarker).Assembly);

    public static IServiceCollection AddBehavior<TBehavior, TRequest, TResponse>(this IServiceCollection services)
        where TBehavior : class, IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        services.AddTransient<IPipelineBehavior<TRequest, TResponse>, TBehavior>();
        
        return services;
    }
}
