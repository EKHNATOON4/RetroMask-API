using FluentValidation;
using RetroMask.Application.Dtos.Points;

namespace RetroMask.Application.Validation.Points;

public class CreatePointValidator : AbstractValidator<CreatePointRequest>
{
    public CreatePointValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
    }
}

public class AddCommentValidator : AbstractValidator<AddCommentRequest>
{
    public AddCommentValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(1000);
    }
}
