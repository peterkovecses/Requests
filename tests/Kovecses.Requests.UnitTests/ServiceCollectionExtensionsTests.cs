namespace Kovecses.Requests.UnitTests;

public class ServiceCollectionExtensionsTests
{
    private readonly IServiceCollection _services = new ServiceCollection();

    [Fact]
    public void AddRequests_WhenCalledWithAssembly_ShouldRegisterHandlers()
    {
        // Act
        _services.AddRequests(typeof(ServiceCollectionExtensionsTests).Assembly);

        // Assert
        var descriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(IRequestHandler<TestRequest, string>));
        Assert.NotNull(descriptor);
        Assert.NotNull(descriptor.ImplementationFactory);
    }

    [Fact]
    public void AddRequests_WhenCalledWithMarkerType_ShouldRegisterHandlers()
    {
        // Act
        _services.AddRequests<ServiceCollectionExtensionsTests>();

        // Assert
        var descriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(IRequestHandler<TestRequest, string>));
        Assert.NotNull(descriptor);
    }

    [Fact]
    public void AddBehavior_ShouldRegisterAsTransient()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddRequests<ServiceCollectionExtensionsTests>();

        // Act
        builder.AddGlobalBehavior(typeof(TestBehavior<,>));

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IPipelineBehavior<,>));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }
}

public record TestRequest : IRequest<string>;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    public Task<string> HandleAsync(TestRequest request, CancellationToken cancellationToken) => Task.FromResult("Response");
}

public class TestBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        => next();
}
