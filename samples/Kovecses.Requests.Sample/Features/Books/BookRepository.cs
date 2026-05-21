namespace Kovecses.Requests.Sample.Features.Books;

internal sealed class BookRepository : IBookRepository
{
    private readonly List<BookDto> _books =
    [
        new(Guid.NewGuid(), "The Fellowship of the Ring", "J.R.R. Tolkien", 25.99m),
        new(Guid.NewGuid(), "The Two Towers", "J.R.R. Tolkien", 25.99m),
        new(Guid.NewGuid(), "The Return of the King", "J.R.R. Tolkien", 25.99m),
        new(Guid.NewGuid(), "Foundation", "Isaac Asimov", 19.99m),
        new(Guid.NewGuid(), "Dune", "Frank Herbert", 22.50m)
    ];

    public IEnumerable<BookDto> GetAll()
        => _books;

    public void Add(BookDto book)
        => _books.Add(book);
}
