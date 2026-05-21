namespace Kovecses.Requests.Sample.Common.ErrorHandling;

public sealed class ValidationExceptionHandler(
    ILogger<ValidationExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        logger.LogValidationException(exception, exception.Message);

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new HttpValidationProblemDetails(validationException.Errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Title = "Validation Failed",
                Detail = "One or more validation errors occurred."
            }
        });
    }
}

internal static partial class ValidationExceptionHandlerLogger
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Validation exception occurred: {Message}")]
    internal static partial void LogValidationException(this ILogger<ValidationExceptionHandler> logger, Exception exception, string message);
}
