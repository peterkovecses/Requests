namespace Kovecses.Requests.Sample.Infrastructure.Persistence;

internal sealed class BookRepository : IBookRepository
{
    private readonly List<BookDto> _books =
    [
        new(Guid.NewGuid(), "The Fellowship of the Ring", "J.R.R. Tolkien", 25.99m, IsActive: true),
        new(Guid.NewGuid(), "The Two Towers", "J.R.R. Tolkien", 25.99m, IsActive: false),
        new(Guid.NewGuid(), "The Return of the King", "J.R.R. Tolkien", 25.99m, IsActive: true),
        new(Guid.NewGuid(), "Foundation", "Isaac Asimov", 19.99m, IsActive: true),
        new(Guid.NewGuid(), "Dune", "Frank Herbert", 22.50m, IsActive: false)
    ];

    public Task<IEnumerable<BookDto>> GetAllAsync(CancellationToken cancellationToken)
        => Task.FromResult<IEnumerable<BookDto>>(_books);

    public Task AddAsync(BookDto book, CancellationToken cancellationToken)
    {
        _books.Add(book);
        
        return Task.CompletedTask;
    }

    public Task<bool> UpdatePriceAsync(Guid id, decimal newPrice, CancellationToken cancellationToken)
    {
        var index = _books.FindIndex(b => b.Id == id);
        
        if (index == -1)
        {
            return Task.FromResult(false);
        }

        _books[index] = _books[index] with { Price = newPrice };
        
        return Task.FromResult(true);
    }
}
