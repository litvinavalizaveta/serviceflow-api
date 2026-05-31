using FluentValidation;
using ServiceFlow.Api.Contracts.Auth;
using ServiceFlow.Api.Security;

namespace ServiceFlow.Api.Validation.Auth;

public sealed class DemoTokenRequestValidator : AbstractValidator<DemoTokenRequest>
{
    public DemoTokenRequestValidator()
    {
        RuleFor(request => request.UserId)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.DisplayName)
            .MaximumLength(200);

        RuleFor(request => request.Role)
            .NotEmpty()
            .Must(role => ServiceFlowRoles.All.Contains(role))
            .WithMessage("Role must be one of: Admin, Agent, Viewer.");
    }
}
