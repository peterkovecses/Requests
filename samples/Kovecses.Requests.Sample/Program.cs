var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IBookRepository, BookRepository>();
builder.Services.AddScoped<IValidator<CreateBookCommand>, CreateBookCommandValidator>();

builder.Services.AddRequests(typeof(Program).Assembly);
builder.Services.AddBehavior<LoggingBehavior<GetBooksQuery, IEnumerable<BookDto>>, GetBooksQuery, IEnumerable<BookDto>>();
builder.Services.AddBehavior<LoggingBehavior<CreateBookCommand, BookDto>, CreateBookCommand, BookDto>();
builder.Services.AddBehavior<ValidationBehavior<CreateBookCommand, BookDto>, CreateBookCommand, BookDto>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();
app.UseExceptionHandler();
app.MapEndpoints();

app.Run();
