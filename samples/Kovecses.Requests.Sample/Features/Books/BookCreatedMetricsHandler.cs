namespace Kovecses.Requests.Sample.Features.Books;

internal sealed class BookCreatedMetricsHandler : IEventHandler<BookCreatedEvent>
{
    public const string MeterName = "Kovecses.Requests.Sample";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> BooksCreated = Meter.CreateCounter<long>("books.created");

    public Task HandleAsync(BookCreatedEvent @event, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BooksCreated.Add(1);

        return Task.CompletedTask;
    }
}
