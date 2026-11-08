namespace Kovecses.Requests.Sample.Features.Books;

internal sealed class BookCreatedAuditHandler(ILogger<BookCreatedAuditHandler> logger)
    : IEventHandler<BookCreatedEvent>
{
    public Task HandleAsync(BookCreatedEvent @event, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Book created: {BookId} - {Title} by {Author}",
            @event.Book.Id,
            @event.Book.Title,
            @event.Book.Author);

        return Task.CompletedTask;
    }
}
