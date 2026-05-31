using FluentValidation;
using ServiceFlow.Api.Contracts.Clients;

namespace ServiceFlow.Api.Validation.Clients;

public sealed class CreateClientRequestValidator : AbstractValidator<CreateClientRequest>
{
    public CreateClientRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(request => request.CompanyName)
            .MaximumLength(200);
    }
}
