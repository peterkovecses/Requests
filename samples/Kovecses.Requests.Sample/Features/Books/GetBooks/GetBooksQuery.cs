namespace Kovecses.Requests.Sample.Features.Books.GetBooks;

public record GetBooksQuery : IRequest<IEnumerable<BookDto>>;
