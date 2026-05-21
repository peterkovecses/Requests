# Kovecses.Requests

High-performance, "no-magic" light mediator implementation for .NET 8, 9, and 10.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
![.NET 8.0, 9.0, 10.0](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-blue)

If you find this project useful, please consider giving it a ⭐ **Star** on GitHub!

## The Three Core Principles

Kovecses.Requests was built with a clear focus on three main goals:

1.  **High Performance:** Minimal allocations and zero overhead during request processing. No runtime reflection during execution—everything is resolved by the native .NET DI container.
2.  **No Magic / Transparent Debugging:** Direct handler injection. No "invisible" dispatchers. If you want to see the implementation, just press `F12` on the handler in your endpoint.
3.  **Clean Architecture with Pipeline Support:** Enjoy all the benefits of the Mediator pattern (cross-cutting concerns like logging and validation) without the runtime cost and complexity of a central dispatcher.

## Installation

```bash
dotnet add package Kovecses.Requests
```

## Basic Usage

### 1. Define Request and Response
```csharp
public record GetBooksQuery : IRequest<IEnumerable<BookDto>>;
```

### 2. Implement Handler
```csharp
internal sealed class GetBooksHandler(IBookRepository repository) 
    : IRequestHandler<GetBooksQuery, IEnumerable<BookDto>>
{
    public Task<IEnumerable<BookDto>> Handle(GetBooksQuery request, CancellationToken cancellationToken)
        => repository.GetAllAsync(cancellationToken);
}
```

### 3. Inject and Use in Minimal API
```csharp
app.MapGet("books", async (
    IRequestHandler<GetBooksQuery, IEnumerable<BookDto>> handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.Handle(new GetBooksQuery(), cancellationToken);
    
    return Results.Ok(result);
});
```

## Advanced Registration & Behaviors

The `IRequestsBuilder` provides a fluent API to register your handlers and cross-cutting concerns (behaviors).

### Registration Options
```csharp
builder.Services.AddRequests<Program>()
    // 1. Global Behavior: Applies to EVERY request
    .AddGlobalBehavior(typeof(LoggingBehavior<,>))
    
    // 2. Interface-based Behavior: Applies only to requests implementing IValidatable
    .AddBehavior<IValidatable>(typeof(ValidationBehavior<,>))
    
    // 3. Explicit Behavior: Applies ONLY to this specific request
    .AddBehavior<GetBooksQuery, IEnumerable<BookDto>, ActiveOnlyBehavior>();
```

### Example Behavior Implementation
```csharp
public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting request {RequestName}", typeof(TRequest).Name);
        var response = await next();
        logger.LogInformation("Finished request {RequestName}", typeof(TRequest).Name);
        
        return response;
    }
}
```

## License

This project is licensed under the MIT License.
