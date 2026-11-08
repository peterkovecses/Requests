namespace Kovecses.Requests.Sample.Features.Books.CreateBook;

internal sealed class CreateBookHandler(
    IBookRepository repository,
    IEventPublisher<BookCreatedEvent> eventPublisher) : IRequestHandler<CreateBookCommand, BookDto>
{
    public async Task<BookDto> HandleAsync(CreateBookCommand request, CancellationToken cancellationToken)
    {
        var book = new BookDto(Guid.NewGuid(), request.Title, request.Author, request.Price);        
        await repository.AddAsync(book, cancellationToken);
        await eventPublisher.PublishAsync(new BookCreatedEvent(book), cancellationToken);
        
        return book;
    }
}
