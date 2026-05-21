namespace Kovecses.Requests.Sample.Features.Books.GetBooks;

internal static class GetBooksEndpoint
{
    internal static void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("books", HandleAsync)
            .WithName("GetBooks")
            .WithDescription("Gets the list of books.")
            .Produces<IEnumerable<BookDto>>();

    private static async Task<IResult> HandleAsync(
        IRequestHandler<GetBooksQuery, IEnumerable<BookDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetBooksQuery(), cancellationToken);

        return Results.Ok(result);
    }
}
