namespace Kovecses.Requests;

internal sealed class PipelineRequestHandler<TRequest, TResponse>(
    IRequestHandler<TRequest, TResponse> inner,
    IEnumerable<IPipelineBehavior<TRequest, TResponse>> behaviors)
    : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IPipelineBehavior<TRequest, TResponse>[] _behaviors = [.. behaviors];

    public Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
    {
        if (_behaviors.Length == 0)
        {
            return inner.Handle(request, cancellationToken);
        }

        var index = -1;

        Task<TResponse> Next()
        {
            index++;

            if (index < _behaviors.Length)
            {
                return _behaviors[index].Handle(request, Next, cancellationToken);
            }

            return inner.Handle(request, cancellationToken);
        }

        return Next();
    }
}