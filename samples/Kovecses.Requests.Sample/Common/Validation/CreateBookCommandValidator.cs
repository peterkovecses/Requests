namespace Kovecses.Requests.Sample.Common.Validation;

internal sealed class CreateBookCommandValidator : IValidator<CreateBookCommand>
{
    public bool Validate(CreateBookCommand request, out IDictionary<string, string[]> errors)
    {
        errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors.Add(nameof(request.Title), ["Title is required."]);
        }

        if (string.IsNullOrWhiteSpace(request.Author))
        {
            errors.Add(nameof(request.Author), ["Author is required."]);
        }

        if (request.Price <= 0)
        {
            errors.Add(nameof(request.Price), ["Price must be greater than zero."]);
        }

        return errors.Count is 0;
    }
}
