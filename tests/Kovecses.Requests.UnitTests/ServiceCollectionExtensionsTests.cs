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
}

public record TestRequest : IRequest<string>;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    public Task<string> Handle(TestRequest request, CancellationToken cancellationToken) => Task.FromResult("Response");
}
