namespace Kovecses.Requests.Sample.Features.Books.GetBooks;

internal sealed class GetBooksHandler(IBookRepository repository) : IRequestHandler<GetBooksQuery, IEnumerable<BookDto>>
{
    public Task<IEnumerable<BookDto>> HandleAsync(GetBooksQuery request, CancellationToken cancellationToken)
        => repository.GetAllAsync(cancellationToken);
}
