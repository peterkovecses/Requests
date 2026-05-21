namespace Kovecses.Requests.UnitTests;

public class PipelineRequestHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenNoBehaviors_ShouldCallInnerHandler()
    {
        // Arrange
        var handler = new FakeHandler("Response");
        var sut = new PipelineRequestHandler<TestRequest, string>(handler, []);
        var request = new TestRequest();

        // Act
        var result = await sut.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal("Response", result);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenBehaviorsExist_ShouldCallThemInOrder()
    {
        // Arrange
        var handler = new FakeHandler("Final");
        var executionOrder = new List<string>();

        var behavior1 = new OrderedBehavior("Behavior1", executionOrder);
        var behavior2 = new OrderedBehavior("Behavior2", executionOrder);

        var sut = new PipelineRequestHandler<TestRequest, string>(handler, [behavior1, behavior2]);
        var request = new TestRequest();

        // Act
        var result = await sut.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal("Final", result);
        Assert.Equal(["Before Behavior1", "Before Behavior2", "After Behavior2", "After Behavior1"], executionOrder);
    }

    [Fact]
    public async Task HandleAsync_WhenBehaviorShortCircuits_ShouldNotCallInnerHandler()
    {
        // Arrange
        var handler = new FakeHandler("Final");
        var behavior = new ShortCircuitBehavior("ShortCircuit");

        var sut = new PipelineRequestHandler<TestRequest, string>(handler, [behavior]);

        // Act
        var result = await sut.HandleAsync(new TestRequest(), CancellationToken.None);

        // Assert
        Assert.Equal("ShortCircuit", result);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldPropagateCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        
        var handler = new FakeHandler("Response");
        var behavior = new TokenCheckingBehavior();
        var sut = new PipelineRequestHandler<TestRequest, string>(handler, [behavior]);

        // Act
        await sut.HandleAsync(new TestRequest(), token);

        // Assert
        Assert.True(behavior.TokenReceived.Equals(token));
        Assert.Equal(token, handler.LastToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldBubbleUpExceptions()
    {
        // Arrange
        var handler = new ExceptionHandler(new InvalidOperationException("Inner fail"));
        var sut = new PipelineRequestHandler<TestRequest, string>(handler, []);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.HandleAsync(new TestRequest(), CancellationToken.None));
        Assert.Equal("Inner fail", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_WhenMultipleNextCallsWithMultipleBehaviors_ShouldWorkCorrectly()
    {
        // Arrange
        var handler = new FakeHandler("Response");
        var executionOrder = new List<string>();
        
        var retryBehavior = new RetryBehavior(2); // Meghívja 2x a next-et
        var loggingBehavior = new OrderedBehavior("Logging", executionOrder);
        
        var sut = new PipelineRequestHandler<TestRequest, string>(handler, [retryBehavior, loggingBehavior]);

        // Act
        var result = await sut.HandleAsync(new TestRequest(), CancellationToken.None);

        // Assert
        Assert.Equal("Response", result);
        Assert.Equal(2, handler.CallCount);
        Assert.Equal([
            "Before Logging", "After Logging",
            "Before Logging", "After Logging"
        ], executionOrder);
    }

    private sealed class FakeHandler(string response) : IRequestHandler<TestRequest, string>
    {
        public int CallCount { get; private set; }
        public CancellationToken LastToken { get; private set; }
        public Task<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            CallCount++;
            LastToken = cancellationToken;

            return Task.FromResult(response);
        }
    }

    private sealed class ExceptionHandler(Exception exception) : IRequestHandler<TestRequest, string>
    {
        public Task<string> HandleAsync(TestRequest request, CancellationToken cancellationToken) => throw exception;
    }

    private sealed class OrderedBehavior(string name, List<string> order) : IPipelineBehavior<TestRequest, string>
    {
        public async Task<string> HandleAsync(TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            order.Add($"Before {name}");
            var result = await next();
            order.Add($"After {name}");

            return result;
        }
    }

    private sealed class ShortCircuitBehavior(string response) : IPipelineBehavior<TestRequest, string>
    {
        public Task<string> HandleAsync(TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }

    private sealed class TokenCheckingBehavior : IPipelineBehavior<TestRequest, string>
    {
        public CancellationToken TokenReceived { get; private set; }
        public async Task<string> HandleAsync(TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            TokenReceived = cancellationToken;

            return await next();
        }
    }

    private sealed class RetryBehavior(int count) : IPipelineBehavior<TestRequest, string>
    {
        public async Task<string> HandleAsync(TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            string lastResult = string.Empty;
            for (int i = 0; i < count; i++)
            {
                lastResult = await next();
            }
            
            return lastResult;
        }
    }
}
