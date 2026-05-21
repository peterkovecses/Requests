namespace Kovecses.Requests.Sample.Common.ErrorHandling;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogUnhandledException(exception, exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            Title = "Internal Server Error",
            Detail = environment.IsDevelopment()
                ? exception.Message
                : "An error occurred while processing your request."
        };

        if (environment.IsDevelopment())
        {
            problemDetails.Extensions["exception"] = exception.ToString();
        }

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }
}

internal static partial class GlobalExceptionHandlerLogger
{
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Unhandled exception occurred: {Message}")]
    internal static partial void LogUnhandledException(this ILogger<GlobalExceptionHandler> logger, Exception exception, string message);
}
