namespace Kovecses.Requests.Sample;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IBookRepository, BookRepository>();
        services.AddScoped<IValidator<CreateBookCommand>, CreateBookCommandValidator>();

        services.AddRequests<Program>();
        
        services.AddBehavior<LoggingBehavior<GetBooksQuery, IEnumerable<BookDto>>, GetBooksQuery, IEnumerable<BookDto>>();
        services.AddBehavior<LoggingBehavior<CreateBookCommand, BookDto>, CreateBookCommand, BookDto>();
        services.AddBehavior<ValidationBehavior<CreateBookCommand, BookDto>, CreateBookCommand, BookDto>();

        services.AddProblemDetails();
        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }
}
