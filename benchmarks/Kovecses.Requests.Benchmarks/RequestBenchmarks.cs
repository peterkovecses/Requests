using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.DependencyInjection;
using MediatR;

namespace Kovecses.Requests.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class RequestBenchmarks
{
    private IServiceProvider _serviceProvider = null!;
    private ISender _mediatrSender = null!;
    private readonly PingRequest _request = new();

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();

        // Kovecses.Requests setup
        services.AddRequests<RequestBenchmarks>();

        // MediatR setup
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RequestBenchmarks).Assembly));

        _serviceProvider = services.BuildServiceProvider();
        _mediatrSender = _serviceProvider.GetRequiredService<ISender>();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SimpleRequest")]
    public Task<string> DirectCall()
    {
        return Task.FromResult("Pong");
    }

    [Benchmark]
    [BenchmarkCategory("SimpleRequest")]
    public Task<string> KovecsesRequests()
    {
        // Including DI resolution to simulate Minimal API parameter injection overhead
        var handler = _serviceProvider.GetRequiredService<IRequestHandler<PingRequest, string>>();
        return handler.HandleAsync(_request, CancellationToken.None);
    }

    [Benchmark]
    [BenchmarkCategory("SimpleRequest")]
    public Task<string> MediatR()
    {
        // MediatR's Send internally resolves the handler from DI
        return _mediatrSender.Send(_request, CancellationToken.None);
    }
}

public record PingRequest : IRequest<string>, MediatR.IRequest<string>;

public class PingHandler : IRequestHandler<PingRequest, string>, MediatR.IRequestHandler<PingRequest, string>
{
    public Task<string> HandleAsync(PingRequest request, CancellationToken cancellationToken) => Task.FromResult("Pong");
    
    public Task<string> Handle(PingRequest request, CancellationToken cancellationToken) => Task.FromResult("Pong");
}
