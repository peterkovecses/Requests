namespace Kovecses.Requests.Sample.Common.Interfaces;

public interface IBookRepository
{
    Task<IEnumerable<BookDto>> GetAllAsync(CancellationToken cancellationToken);
    Task AddAsync(BookDto book, CancellationToken cancellationToken);
    Task<bool> UpdatePriceAsync(Guid id, decimal newPrice, CancellationToken cancellationToken);
}
