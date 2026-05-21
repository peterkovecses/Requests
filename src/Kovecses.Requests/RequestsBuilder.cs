using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Kovecses.Requests;

internal sealed class RequestsBuilder(IServiceCollection services, Assembly[] assemblies) : IRequestsBuilder
{
    public IServiceCollection Services => services;

    public IRequestsBuilder AddGlobalBehavior(Type openBehaviorType)
    {
        services.AddTransient(typeof(IPipelineBehavior<,>), openBehaviorType);

        return this;
    }

    public IRequestsBuilder AddBehavior<TInterface>(Type openBehaviorType)
    {
        var requestType = typeof(IRequest<>);
        var interfaceType = typeof(TInterface);

        var requestsWithInterface = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == requestType && interfaceType.IsAssignableFrom(t))
                .Select(i => new { RequestType = t, ResponseType = i.GetGenericArguments()[0] }));

        foreach (var request in requestsWithInterface)
        {
            if (CanApplyBehavior(openBehaviorType, request.RequestType, request.ResponseType))
            {
                var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(request.RequestType, request.ResponseType);
                var implementationType = openBehaviorType.MakeGenericType(request.RequestType, request.ResponseType);
                services.AddTransient(behaviorType, implementationType);
            }
        }

        return this;
    }

    public IRequestsBuilder AddBehavior<TRequest, TResponse, TBehavior>()
        where TRequest : IRequest<TResponse>
        where TBehavior : class, IPipelineBehavior<TRequest, TResponse>
    {
        services.AddTransient<IPipelineBehavior<TRequest, TResponse>, TBehavior>();
        
        return this;
    }

    private static bool CanApplyBehavior(Type openBehaviorType, Type requestType, Type responseType)
    {
        try
        {
            _ = openBehaviorType.MakeGenericType(requestType, responseType);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
