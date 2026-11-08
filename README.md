# Kovecses.Requests

[![NuGet Version](https://img.shields.io/nuget/v/Kovecses.Requests?style=flat-square&logo=nuget)](https://www.nuget.org/packages/Kovecses.Requests)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](https://opensource.org/licenses/MIT)
![.NET 8.0, 9.0, 10.0](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-blue?style=flat-square)

High-performance, "no-magic" light request handling implementation for .NET 8, 9, and 10.

---

### Support the Project
If you find this library useful, please give it a **star** on GitHub! It helps more developers discover the project. ⭐

---

## The Three Core Principles

Kovecses.Requests was built with a clear focus on three main goals:

1.  **High Performance:** Optimized execution path with pre-compiled factories and native .NET DI resolution during injection.
2.  **No Magic / Transparent Debugging:** Direct handler injection. No "invisible" dispatchers or runtime reflection during execution. If you want to see the implementation, just press `F12` on the handler in your endpoint.
3.  **Clean Architecture with Pipeline Support:** Benefit from decoupled cross-cutting concerns (like logging and validation) via a stateless pipeline implementation that fully supports advanced scenarios like **Retry policies** (Polly) and recovery logic.

---

## Installation

Install the package via NuGet:

```bash
dotnet add package Kovecses.Requests
```

---

## Basic Usage

### 1. Define Request and Response
```csharp
using Kovecses.Requests;

public record GetBooksQuery : IRequest<IEnumerable<BookDto>>;
```

### 2. Implement Handler
```csharp
internal sealed class GetBooksHandler(IBookRepository repository) 
    : IRequestHandler<GetBooksQuery, IEnumerable<BookDto>>
{
    public Task<IEnumerable<BookDto>> HandleAsync(GetBooksQuery request, CancellationToken cancellationToken)
        => repository.GetAllAsync(cancellationToken);
}
```

### 3. Register Services in Startup
```csharp
// In Program.cs or Startup configuration
builder.Services.AddRequests<Program>();  // Scans assemblies and registers handlers
```

### 4. Inject and Use in Minimal API
```csharp
// Direct injection of the handler - clean, fast, and F12-able!
app.MapGet("books", async (
    IRequestHandler<GetBooksQuery, IEnumerable<BookDto>> handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(new GetBooksQuery(), cancellationToken);
    
    return Results.Ok(result);
});
```

---

## In-Process Event Messaging

Events let a request handler notify multiple parts of the application without depending directly on them. Publishing is **in-process and awaited**: it does not enqueue work or provide durable delivery.

### Define an event and its handlers

```csharp
public sealed record BookCreatedEvent(BookDto Book) : IEvent;

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

internal sealed class BookCreatedMetricsHandler : IEventHandler<BookCreatedEvent>
{
    private static readonly Meter Meter = new("Kovecses.Requests.Sample");
    private static readonly Counter<long> BooksCreated = Meter.CreateCounter<long>("books.created");

    public Task HandleAsync(BookCreatedEvent @event, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BooksCreated.Add(1);

        return Task.CompletedTask;
    }
}
```

`IEventHandler<in TEvent>` handlers are discovered from the assemblies passed to `AddRequests`; the sample registers both an audit handler and a metrics handler. Its metrics are exported through the sample's OpenTelemetry console exporter.

The sample's `CreateBookHandler` adds the book, then awaits publishing. If a subscriber fails, it logs the failure and rethrows it rather than reporting a successful request:

```csharp
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
```

Set a default strategy and, optionally, override it for one event type. The event-specific setting takes precedence:

```csharp
builder.Services.AddRequests<Program>()
    .UseEventPublishStrategy(EventPublishStrategy.Sequential) // default for all events
    .UseEventPublishStrategy<BookCreatedEvent>(EventPublishStrategy.Parallel);
```

Sequential publishing awaits handlers in DI registration order and stops at the first handler error. If handlers are registered explicitly, this is their registration order. Assembly scanning registers handlers in scan order, for which the library does not guarantee a stable order across builds or environments.

Parallel publishing calls each handler synchronously in DI registration order to obtain its task, then awaits all of them; their completion order is not guaranteed. It waits for every invoked handler even when one or more fail. If any non-cancellation exception occurs, all captured exceptions (including cancellation exceptions) are reported in an `AggregateException`, even when there is only one failure. If every failure is an `OperationCanceledException`, the first captured cancellation exception is propagated directly.

Publishing an event with no registered subscribers throws `NoEventSubscribersException`; its `EventType` property identifies the event type. A successful `PublishAsync` means at least one handler was registered and all handlers completed successfully. Handlers are resolved for the publisher's exact generic event type; DI does not automatically include handlers registered for a different base or derived event type. A token already canceled when `PublishAsync` starts takes precedence: cancellation is thrown before the no-subscriber check. In parallel mode, cancellation after that initial check does not stop the launch loop from invoking remaining handlers; the token is passed to each handler, which chooses when and whether to observe it. A cancellation occurring after the last cancellation check may go unobserved, so publishing can still complete successfully. Cancellation and handler errors can happen after other handlers have performed work.

Handlers and the publisher are resolved in the same DI scope; publishing does not create a scope. In parallel mode, scoped dependencies may therefore be shared by concurrently executing handlers. Make shared dependencies safe for concurrent use, or use sequential publishing. Publishing awaits completion but does not guarantee delivery: a process shutdown, failure, or cancellation can prevent work from completing, and completed handler side effects are not rolled back. The sample saves the book before publishing, so a handler failure is logged and rethrown, but the repository change is not rolled back; production applications that require atomic persistence and event handling need an appropriate transaction/outbox design.

---

## Usage in Background Services

When injecting handlers into Singleton services (like `BackgroundService`), always use `IServiceScopeFactory` to resolve the handler within a new scope. This prevents captive dependencies and ensures the Transient pipeline behaves correctly.

```csharp
using var scope = scopeFactory.CreateScope();
var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<MyRequest, MyResponse>>();
```

---

## Advanced Registration & Behaviors

The `IRequestsBuilder` provides a fluent API to register your handlers and cross-cutting concerns (behaviors).

### OpenTelemetry & Distributed Tracing
Monitoring and observability are essential for modern cloud-native applications. Kovecses.Requests provides built-in support for OpenTelemetry via an optional behavior.

1. **Enable the behavior:**
```csharp
builder.Services.AddRequests<Program>()
    .AddOpenTelemetry(); // Enables ActivitySource-based tracing for all requests
```

2. **Configure OpenTelemetry to collect the traces:**
To actually see the traces, you must register the `Kovecses.Requests` source in your OpenTelemetry configuration:

```csharp
services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("Kovecses.Requests") // <-- Required to collect library traces
        .AddAspNetCoreInstrumentation()
        .AddConsoleExporter());
```

This registers a global `OpenTelemetryBehavior` that automatically starts an `Activity` for each request. It captures the request type and handles exception reporting.

### Registration Options
```csharp
builder.Services.AddRequests<Program>()
    // 1. Global Behavior: Applies to EVERY request
    .AddGlobalBehavior(typeof(LoggingBehavior<,>))
    
    // 2. Interface-based Behavior: Applies only to requests implementing IValidatable marker interface
    // Example: public record UpdateBookCommand : IRequest<BookDto>, IValidatable;
    .AddBehavior<IValidatable>(typeof(ValidationBehavior<,>))
    
    // 3. Explicit Behavior: Applies ONLY to this specific request
    .AddBehavior<GetBooksQuery, IEnumerable<BookDto>, ActiveOnlyBehavior>();
```

> **Note:** All behaviors are registered with **Transient** lifetime to ensure thread safety and avoid accidental state sharing in the pipeline. This also optimizes performance as the DI container can resolve them efficiently.

### Example Behavior Implementation
```csharp
public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting request {RequestName}", typeof(TRequest).Name);
        
        // The cancellation token is automatically propagated through the pipeline closure
        var response = await next();
        
        logger.LogInformation("Finished request {RequestName}", typeof(TRequest).Name);

        return response;
    }
}
```

---

## Benchmarks

The following benchmarks compare **Kovecses.Requests** with **MediatR** using **BenchmarkDotNet**. 

*Environment: .NET 10.0.1, macOS 26.6 (Darwin 25.6.0), Apple M1*

> **Disclaimer:** This comparison is not "apples-to-apples" in terms of features. MediatR is a feature-rich library with a central dispatcher and dynamic resolution. Kovecses.Requests intentionally opts for direct injection and native DI resolution. These benchmarks illustrate the **"infrastructure tax"** (runtime overhead) you can avoid by choosing a no-magic, direct-injection approach.

### Simple Request/Response
Measures resolving a handler from DI and executing it.

| Method           | Mean       | Ratio | Allocated |
|----------------- |-----------:|------:|----------:|
| DirectCall       |   7.831 ns |  1.00 |      72 B |
| KovecsesRequests |  56.332 ns |  7.19 |     168 B |
| MediatR          |  81.454 ns | 10.40 |     200 B |

### Pipeline (2 Behaviors)
Measures resolving a handler with a pipeline (2 behaviors) and executing it.

| Method                          | Mean       | Ratio | Allocated |
|-------------------------------- |-----------:|------:|----------:|
| DirectCall                      |   8.155 ns |  1.00 |      72 B |
| KovecsesRequests_With2Behaviors | 139.091 ns | 17.06 |     672 B |
| MediatR_With2Behaviors          | 177.913 ns | 21.82 |     728 B |

*Note: In these benchmarks, `KovecsesRequests` includes manual DI resolution (`GetRequiredService`) to accurately reflect the overhead in a real-world Minimal API endpoint.*

---

## License

This project is licensed under the MIT License.
