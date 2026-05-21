using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Kovecses.Requests;

public interface IRequestsBuilder
{
    IServiceCollection Services { get; }
    IRequestsBuilder AddGlobalBehavior(Type openBehaviorType);
    IRequestsBuilder AddBehavior<TInterface>(Type openBehaviorType);
    IRequestsBuilder AddBehavior<TRequest, TResponse, TBehavior>()
        where TRequest : IRequest<TResponse>
        where TBehavior : class, IPipelineBehavior<TRequest, TResponse>;
}

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
            var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(request.RequestType, request.ResponseType);
            var implementationType = openBehaviorType.MakeGenericType(request.RequestType, request.ResponseType);
            services.AddTransient(behaviorType, implementationType);
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
}
