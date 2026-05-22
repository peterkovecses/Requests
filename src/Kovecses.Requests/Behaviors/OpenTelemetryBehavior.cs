using System.Diagnostics;

namespace Kovecses.Requests.Behaviors;

internal sealed class OpenTelemetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly ActivitySource ActivitySource = new("Kovecses.Requests");

    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity(request.GetType().Name);
        
        activity?.SetTag("request.type", typeof(TRequest).FullName);

        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            if (activity is not null)
            {
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection 
                { 
                    { "exception.type", ex.GetType().FullName },
                    { "exception.message", ex.Message },
                    { "exception.stacktrace", ex.StackTrace }
                }));
            }
            throw;
        }
    }
}
