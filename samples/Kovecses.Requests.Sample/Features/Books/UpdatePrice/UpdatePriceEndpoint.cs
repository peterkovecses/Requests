namespace Kovecses.Requests.Sample.Features.Books.UpdatePrice;

internal static class UpdatePriceEndpoint
{
    internal static void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPatch("books/{id:guid}/price", HandleAsync)
            .WithName("UpdateBookPrice")
            .WithDescription("Updates the price of a book.")
            .Produces<bool>()
            .ProducesValidationProblem();

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdatePriceRequest request,
        IRequestHandler<UpdatePriceCommand, bool> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePriceCommand(id, request.NewPrice);
        var result = await handler.Handle(command, cancellationToken);
        
        return result 
            ? Results.NoContent() 
            : Results.NotFound();
    }
}
