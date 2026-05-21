namespace Kovecses.Requests.Sample.Features.Books.UpdatePrice;

public record UpdatePriceRequest(decimal NewPrice);

public record UpdatePriceCommand(Guid Id, decimal NewPrice) : IRequest<bool>, IValidatable;
