namespace Kovecses.Requests.Sample.Features.Books.CreateBook;

internal sealed class CreateBookHandler(
    IBookRepository repository,
    IEventPublisher<BookCreatedEvent> eventPublisher) : IRequestHandler<CreateBookCommand, BookDto>
{
    public async Task<BookDto> HandleAsync(CreateBookCommand request, CancellationToken cancellationToken)
    {
        var book = new BookDto(Guid.NewGuid(), request.Title, request.Author, request.Price);
        await repository.AddAsync(book, cancellationToken);

        try
        {
            await eventPublisher.PublishAsync(new BookCreatedEvent(book), cancellationToken);
        }
        catch (Exception)
        {
            // Apply application-specific failure handling here; rethrow so the request does not report success.
            throw;
        }

        return book;
    }
}
