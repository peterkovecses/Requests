namespace Kovecses.Requests.Sample.Common.Validation;

internal sealed class UpdatePriceCommandValidator : IValidator<UpdatePriceCommand>
{
    public bool Validate(UpdatePriceCommand request, out IDictionary<string, string[]> errors)
    {
        errors = new Dictionary<string, string[]>();

        if (request.Id == Guid.Empty)
        {
            errors.Add(nameof(request.Id), ["Book ID is required."]);
        }

        if (request.NewPrice <= 0)
        {
            errors.Add(nameof(request.NewPrice), ["New price must be greater than zero."]);
        }

        return errors.Count is 0;
    }
}
