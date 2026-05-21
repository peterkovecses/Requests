var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRequests(typeof(Program).Assembly);

var app = builder.Build();
GetBooksEndpoint.MapEndpoint(app);

app.Run();
