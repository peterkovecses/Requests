namespace Kovecses.Requests.Sample.Features.Books.GetBooks;

public class ActiveOnlyBehavior : IPipelineBehavior<GetBooksQuery, IEnumerable<BookDto>>
{
    public async Task<IEnumerable<BookDto>> HandleAsync(
        GetBooksQuery request,
        RequestHandlerDelegate<IEnumerable<BookDto>> next,
        CancellationToken cancellationToken)
    {
        var books = await next();
        
        return books.Where(book => book.IsActive);
    }
}
