using System.Diagnostics;
using Kovecses.Requests.Behaviors;

namespace Kovecses.Requests.UnitTests;

public class OpenTelemetryBehaviorTests
{
    [Fact]
    public async Task HandleAsync_ShouldCreateActivity()
    {
        // Arrange
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "Kovecses.Requests",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = a => activities.Add(a)
        };
        ActivitySource.AddActivityListener(listener);

        var behavior = new OpenTelemetryBehavior<TestRequest, string>();
        var request = new TestRequest();
        var nextCalled = false;
        Task<string> next()
        {
            nextCalled = true;
            return Task.FromResult("Response");
        }

        // Act
        await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
        Assert.Single(activities);
        var activity = activities[0];
        Assert.Equal(nameof(TestRequest), activity.DisplayName);
        Assert.Equal(typeof(TestRequest).FullName, activity.GetTagItem("request.type"));
    }

    [Fact]
    public async Task HandleAsync_WhenExceptionOccurs_ShouldSetErrorStatus()
    {
        // Arrange
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "Kovecses.Requests",
            Sample = (ref _) => ActivitySamplingResult.AllData,
            ActivityStopped = a => activities.Add(a)
        };
        ActivitySource.AddActivityListener(listener);

        var behavior = new OpenTelemetryBehavior<TestRequest, string>();
        var request = new TestRequest();
        RequestHandlerDelegate<string> next = () => throw new InvalidOperationException("Test Error");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.HandleAsync(request, next, CancellationToken.None));

        Assert.Single(activities);
        var activity = activities[0];
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("Test Error", activity.StatusDescription);
        
        var exceptionEvent = activity.Events.FirstOrDefault(e => e.Name == "exception");
        Assert.NotEqual(default, exceptionEvent);
        Assert.Equal(typeof(InvalidOperationException).FullName, exceptionEvent.Tags.FirstOrDefault(t => t.Key == "exception.type").Value);
        Assert.Equal("Test Error", exceptionEvent.Tags.FirstOrDefault(t => t.Key == "exception.message").Value);
    }

    [Fact]
    public void AddOpenTelemetry_ShouldRegisterBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddRequests(typeof(TestRequest).Assembly);

        // Act
        builder.AddOpenTelemetry();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TestRequest, string>>();
        Assert.Contains(behaviors, b => b.GetType().GetGenericTypeDefinition() == typeof(OpenTelemetryBehavior<,>));
    }
}
