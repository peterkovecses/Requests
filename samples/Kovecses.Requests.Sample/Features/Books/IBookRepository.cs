namespace Kovecses.Requests.Sample.Features.Books;

public interface IBookRepository
{
    IEnumerable<BookDto> GetAll();
    void Add(BookDto book);
}
