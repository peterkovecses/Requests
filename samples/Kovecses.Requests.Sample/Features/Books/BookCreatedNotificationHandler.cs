namespace Kovecses.Requests.Sample.Features.Books;

internal sealed class BookCreatedNotificationHandler(ILogger<BookCreatedNotificationHandler> logger)
    : IEventHandler<BookCreatedEvent>
{
    public Task HandleAsync(BookCreatedEvent @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Catalog notification prepared for the new book {BookId}", @event.Book.Id);

        return Task.CompletedTask;
    }
}
