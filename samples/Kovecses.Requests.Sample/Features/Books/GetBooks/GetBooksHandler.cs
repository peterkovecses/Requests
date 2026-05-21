namespace Kovecses.Requests.Sample.Features.Books.GetBooks;

internal sealed class GetBooksHandler : IRequestHandler<GetBooksQuery, IEnumerable<BookDto>>
{
    private static readonly List<BookDto> Books =
    [
        new(Guid.NewGuid(), "The Fellowship of the Ring", "J.R.R. Tolkien", 25.99m),
        new(Guid.NewGuid(), "The Two Towers", "J.R.R. Tolkien", 25.99m),
        new(Guid.NewGuid(), "The Return of the King", "J.R.R. Tolkien", 25.99m),
        new(Guid.NewGuid(), "Foundation", "Isaac Asimov", 19.99m),
        new(Guid.NewGuid(), "Dune", "Frank Herbert", 22.50m)
    ];

    public Task<IEnumerable<BookDto>> Handle(GetBooksQuery request, CancellationToken cancellationToken)
        => Task.FromResult(Books.AsEnumerable());
}
