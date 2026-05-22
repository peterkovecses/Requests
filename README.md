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

1.  **High Performance:** Optimized execution path with minimal allocations. Everything is resolved by the native .NET DI container at startup.
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

## Advanced Registration & Behaviors

The `IRequestsBuilder` provides a fluent API to register your handlers and cross-cutting concerns (behaviors).

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

*Environment: .NET 10.0.1, macOS 26.5 (Darwin 25.5.0), Apple M1*

> **Disclaimer:** This comparison is not "apples-to-apples" in terms of features. MediatR is a feature-rich library with a central dispatcher and dynamic resolution. Kovecses.Requests intentionally opts for direct injection and native DI resolution. These benchmarks illustrate the **"infrastructure tax"** (runtime overhead) you can avoid by choosing a no-magic, direct-injection approach.

### Simple Request/Response
Measures resolving a handler from DI and executing it.

| Method           | Mean       | Ratio | Allocated |
|----------------- |-----------:|------:|----------:|
| DirectCall       |   8.031 ns |  1.00 |      72 B |
| KovecsesRequests |  60.533 ns |  7.54 |     168 B |
| MediatR          |  81.411 ns | 10.14 |     200 B |

### Pipeline (2 Behaviors)
Measures resolving a handler with a pipeline (2 behaviors) and executing it.

| Method                          | Mean       | Ratio | Allocated |
|-------------------------------- |-----------:|------:|----------:|
| DirectCall                      |   8.074 ns |  1.00 |      72 B |
| KovecsesRequests_With2Behaviors | 148.723 ns | 18.43 |     624 B |
| MediatR_With2Behaviors          | 182.772 ns | 22.65 |     728 B |

*Note: In these updated benchmarks, `KovecsesRequests` includes manual DI resolution (`GetRequiredService`) to accurately reflect the overhead in a real-world Minimal API endpoint.*

---

## License

This project is licensed under the MIT License.
