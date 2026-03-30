using FluentValidation;
using Venturacom.Application.Auth.Commands;

namespace Venturacom.Application.Auth.Validators;

public sealed class GoogleLoginCommandValidator : AbstractValidator<GoogleLoginCommand>
{
    public GoogleLoginCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.IdToken).NotEmpty();
    }
}
