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
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType)
                .Select(i => new { ServiceType = i, ImplementationType = t }));

        foreach (var handler in handlers)
        {
            services.TryAddTransient(handler.ServiceType, handler.ImplementationType);
        }

        return services;
    }
}
