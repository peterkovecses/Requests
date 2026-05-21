namespace Kovecses.Requests.Sample.Features.Books.CreateBook;

public record CreateBookCommand(string Title, string Author, decimal Price) : IRequest<BookDto>, IValidatable;
