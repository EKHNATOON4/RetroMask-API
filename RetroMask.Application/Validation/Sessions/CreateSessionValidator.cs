using FluentValidation;
using RetroMask.Application.Dtos.Sessions;

namespace RetroMask.Application.Validation.Sessions;

public class CreateSessionValidator : AbstractValidator<CreateSessionRequest>
{
    public CreateSessionValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description != null);
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.MaxVotesPerUser).InclusiveBetween(1, 20);
    }
}
