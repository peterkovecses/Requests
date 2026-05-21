namespace Kovecses.Requests.Sample.Common.Extensions;

public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        GetBooksEndpoint.MapEndpoint(app);
        CreateBookEndpoint.MapEndpoint(app);

        return app;
    }
}
