namespace Kovecses.Requests.Sample.Features.Books.CreateBook;

internal static class CreateBookEndpoint
{
    internal static void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("books", HandleAsync)
            .WithName("CreateBook")
            .WithDescription("Creates a new book.")
            .Produces<BookDto>()
            .ProducesValidationProblem();

    private static async Task<IResult> HandleAsync(
        CreateBookRequest request,
        IRequestHandler<CreateBookCommand, BookDto> handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateBookCommand(request.Title, request.Author, request.Price);
        var result = await handler.HandleAsync(command, cancellationToken);
        
        return Results.Created($"/books/{result.Id}", result);
    }
}
