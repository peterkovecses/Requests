namespace Kovecses.Requests.Sample.Features.Books.CreateBook;

internal sealed class CreateBookHandler(
    IBookRepository repository,
    ILogger<CreateBookHandler> logger,
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Book {BookId} was saved, but its creation event was not fully processed.",
                book.Id);
            throw;
        }
        
        return book;
    }
}
