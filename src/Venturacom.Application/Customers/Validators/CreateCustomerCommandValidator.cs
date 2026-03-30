using FluentValidation;
using Venturacom.Application.Customers.Commands;

namespace Venturacom.Application.Customers.Validators;

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Identification).NotEmpty().MaximumLength(13);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
    }
}
