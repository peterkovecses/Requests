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

        RequestHandlerDelegate<TResponse> GetNextDelegate(int currentIndex)
        {
            if (currentIndex >= _behaviors.Length)
            {
                return () => inner.HandleAsync(request, cancellationToken);
            }

            return () => _behaviors[currentIndex].HandleAsync(request, GetNextDelegate(currentIndex + 1), cancellationToken);
        }

        return GetNextDelegate(0)();
    }
}