namespace Kovecses.Requests;

internal sealed class PipelineRequestHandler<TRequest, TResponse>(
    IRequestHandler<TRequest, TResponse> inner,
    IEnumerable<IPipelineBehavior<TRequest, TResponse>> behaviors)
    : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
    {
        Task<TResponse> handler() => inner.Handle(request, cancellationToken);

        return behaviors
            .Reverse()
            .Aggregate((RequestHandlerDelegate<TResponse>)handler, (next, behavior) => () => behavior.Handle(request, next, cancellationToken))();
    }
}