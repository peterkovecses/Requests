namespace Kovecses.Requests.Sample.Features.Books;

public sealed record BookCreatedEvent(BookDto Book) : IEvent;
