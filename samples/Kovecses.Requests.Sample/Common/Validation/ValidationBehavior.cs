namespace Kovecses.Requests.Sample.Common.Validation;

public interface IValidator<in TRequest>
{
    bool Validate(TRequest request, out IDictionary<string, string[]> errors);
}

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var failures = new Dictionary<string, string[]>();

        foreach (var validator in validators)
        {
            if (!validator.Validate(request, out var errors))
            {
                foreach (var error in errors)
                {
                    failures[error.Key] = error.Value;
                }
            }
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}

public class ValidationException(IDictionary<string, string[]> errors) : Exception("Validation failed")
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}
