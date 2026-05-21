namespace Kovecses.Requests.Sample;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IBookRepository, BookRepository>();
        services.AddScoped<IValidator<CreateBookCommand>, CreateBookCommandValidator>();
        services.AddScoped<IValidator<UpdatePriceCommand>, UpdatePriceCommandValidator>();

        services.AddRequests<Program>()
            .AddGlobalBehavior(typeof(LoggingBehavior<,>))
            .AddBehavior<IValidatable>(typeof(ValidationBehavior<,>))
            .AddBehavior<GetBooksQuery, IEnumerable<BookDto>, ActiveOnlyBehavior>();

        services.AddProblemDetails();
        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }
}
