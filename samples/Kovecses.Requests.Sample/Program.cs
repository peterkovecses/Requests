var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRequests(typeof(Program).Assembly);
builder.Services.AddBehavior<LoggingBehavior<GetBooksQuery, IEnumerable<BookDto>>, GetBooksQuery, IEnumerable<BookDto>>();

var app = builder.Build();
GetBooksEndpoint.MapEndpoint(app);

app.Run();
