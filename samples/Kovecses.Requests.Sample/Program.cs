var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationServices();

var app = builder.Build();
app.UseExceptionHandler();
app.MapEndpoints();

app.Run();
