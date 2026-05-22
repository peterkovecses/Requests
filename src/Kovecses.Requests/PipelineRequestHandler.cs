namespace Kovecses.Requests;

internal sealed class PipelineRequestHandler<TRequest, TResponse>(
    IRequestHandler<TRequest, TResponse> inner,
    IEnumerable<IPipelineBehavior<TRequest, TResponse>> behaviors)
    : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IPipelineBehavior<TRequest, TResponse>[] _behaviors =
        behaviors as IPipelineBehavior<TRequest, TResponse>[] ?? [.. behaviors];

    public Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken)
    {
        if (_behaviors.Length == 0)
        {
            return inner.HandleAsync(request, cancellationToken);
        }

        RequestHandlerDelegate<TResponse> next = () => inner.HandleAsync(request, cancellationToken);

        for (int i = _behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = _behaviors[i];
            var nextDelegate = next;
            next = () => behavior.HandleAsync(request, nextDelegate, cancellationToken);
        }

        return next();
    }
}