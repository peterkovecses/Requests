using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.DependencyInjection;
using MediatR;

namespace Kovecses.Requests.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class PipelineBenchmarks
{
    private IServiceProvider _serviceProvider = null!;
    private ISender _mediatrSender = null!;
    private readonly PingRequest _request = new();

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();

        // Kovecses.Requests setup
        services.AddRequests<PipelineBenchmarks>()
            .AddGlobalBehavior(typeof(RequestsLoggingBehavior<,>))
            .AddGlobalBehavior(typeof(RequestsValidationBehavior<,>));

        // MediatR setup
        services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssembly(typeof(PipelineBenchmarks).Assembly);
            cfg.AddOpenBehavior(typeof(MediatrLoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(MediatrValidationBehavior<,>));
        });

        _serviceProvider = services.BuildServiceProvider();
        _mediatrSender = _serviceProvider.GetRequiredService<ISender>();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Pipeline")]
    public Task<string> DirectCall()
    {
        return Task.FromResult("Pong");
    }

    [Benchmark]
    [BenchmarkCategory("Pipeline")]
    public Task<string> KovecsesRequests_With2Behaviors()
    {
        // Including DI resolution to simulate Minimal API parameter injection overhead
        var handler = _serviceProvider.GetRequiredService<IRequestHandler<PingRequest, string>>();
        return handler.HandleAsync(_request, CancellationToken.None);
    }

    [Benchmark]
    [BenchmarkCategory("Pipeline")]
    public Task<string> MediatR_With2Behaviors()
    {
        // MediatR's Send internally resolves the handler and behaviors from DI
        return _mediatrSender.Send(_request, CancellationToken.None);
    }
}

// Kovecses.Requests behaviors
public class RequestsLoggingBehavior<TRequest, TResponse> : Kovecses.Requests.IPipelineBehavior<TRequest, TResponse>
    where TRequest : Kovecses.Requests.IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(TRequest request, Kovecses.Requests.RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        return await next();
    }
}

public class RequestsValidationBehavior<TRequest, TResponse> : Kovecses.Requests.IPipelineBehavior<TRequest, TResponse>
    where TRequest : Kovecses.Requests.IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(TRequest request, Kovecses.Requests.RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        return await next();
    }
}

// MediatR behaviors
public class MediatrLoggingBehavior<TRequest, TResponse> : MediatR.IPipelineBehavior<TRequest, TResponse>
    where TRequest : MediatR.IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, MediatR.RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        return await next();
    }
}

public class MediatrValidationBehavior<TRequest, TResponse> : MediatR.IPipelineBehavior<TRequest, TResponse>
    where TRequest : MediatR.IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, MediatR.RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        return await next();
    }
}
