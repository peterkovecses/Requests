namespace Kovecses.Requests.UnitTests;

public class PipelineRequestHandlerTests
{
    [Fact]
    public async Task Handle_WhenNoBehaviors_ShouldCallInnerHandler()
    {
        // Arrange
        var handler = new FakeHandler("Response");
        var sut = new PipelineRequestHandler<TestRequest, string>(handler, []);
        var request = new TestRequest();

        // Act
        var result = await sut.Handle(request, CancellationToken.None);

        // Assert
        Assert.Equal("Response", result);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task Handle_WhenBehaviorsExist_ShouldCallThemInOrder()
    {
        // Arrange
        var handler = new FakeHandler("Final");
        var executionOrder = new List<string>();

        var behavior1 = new LoggingBehavior("Behavior1", executionOrder);
        var behavior2 = new LoggingBehavior("Behavior2", executionOrder);

        var sut = new PipelineRequestHandler<TestRequest, string>(handler, [behavior1, behavior2]);
        var request = new TestRequest();

        // Act
        var result = await sut.Handle(request, CancellationToken.None);

        // Assert
        Assert.Equal("Final", result);
        Assert.Equal(["Behavior1", "Behavior2"], executionOrder);
    }

    [Fact]
    public async Task Handle_WhenBehaviorShortCircuits_ShouldNotCallInnerHandler()
    {
        // Arrange
        var handler = new FakeHandler("Final");
        var behavior = new ShortCircuitBehavior("ShortCircuit");

        var sut = new PipelineRequestHandler<TestRequest, string>(handler, [behavior]);

        // Act
        var result = await sut.Handle(new TestRequest(), CancellationToken.None);

        // Assert
        Assert.Equal("ShortCircuit", result);
        Assert.Equal(0, handler.CallCount);
    }

    private sealed class FakeHandler(string response) : IRequestHandler<TestRequest, string>
    {
        public int CallCount { get; private set; }
        public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
        {
            CallCount++;
            
            return Task.FromResult(response);
        }
    }

    private sealed class LoggingBehavior(string name, List<string> order) : IPipelineBehavior<TestRequest, string>
    {
        public async Task<string> Handle(TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            order.Add(name);

            return await next();
        }
    }

    private sealed class ShortCircuitBehavior(string response) : IPipelineBehavior<TestRequest, string>
    {
        public Task<string> Handle(TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken) 
            => Task.FromResult(response);
    }
}
