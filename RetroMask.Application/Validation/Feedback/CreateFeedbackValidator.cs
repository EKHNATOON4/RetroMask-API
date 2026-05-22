using FluentValidation;
using RetroMask.Application.Dtos.Feedback;

namespace RetroMask.Application.Validation.Feedback;

public class CreateFeedbackValidator : AbstractValidator<CreateFeedbackRequest>
{
    public CreateFeedbackValidator()
    {
        RuleFor(x => x.ReceiverId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).When(x => x.Rating.HasValue);
    }
}
