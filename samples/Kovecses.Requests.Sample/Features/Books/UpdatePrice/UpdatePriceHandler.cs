namespace Kovecses.Requests.Sample.Features.Books.UpdatePrice;

internal sealed class UpdatePriceHandler(IBookRepository repository) : IRequestHandler<UpdatePriceCommand, bool>
{
    public Task<bool> Handle(UpdatePriceCommand request, CancellationToken cancellationToken)
        => repository.UpdatePriceAsync(request.Id, request.NewPrice, cancellationToken);
}
