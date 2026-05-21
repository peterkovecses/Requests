namespace Kovecses.Requests.Sample.Features.Books.CreateBook;

internal sealed class CreateBookHandler(IBookRepository repository) : IRequestHandler<CreateBookCommand, BookDto>
{
    public Task<BookDto> Handle(CreateBookCommand request, CancellationToken cancellationToken)
    {
        var book = new BookDto(Guid.NewGuid(), request.Title, request.Author, request.Price);        
        repository.Add(book);
        
        return Task.FromResult(book);
    }
}
