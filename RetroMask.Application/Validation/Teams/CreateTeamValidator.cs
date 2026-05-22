using FluentValidation;
using RetroMask.Application.Dtos.Teams;

namespace RetroMask.Application.Validation.Teams;

public class CreateTeamValidator : AbstractValidator<CreateTeamRequest>
{
    public CreateTeamValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);
    }
}

public class InviteMemberValidator : AbstractValidator<InviteMemberRequest>
{
    public InviteMemberValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
