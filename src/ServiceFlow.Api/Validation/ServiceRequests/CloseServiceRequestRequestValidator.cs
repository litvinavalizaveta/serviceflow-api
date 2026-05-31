using FluentValidation;
using ServiceFlow.Api.Contracts.ServiceRequests;

namespace ServiceFlow.Api.Validation.ServiceRequests;

public sealed class CloseServiceRequestRequestValidator : AbstractValidator<CloseServiceRequestRequest>
{
    public CloseServiceRequestRequestValidator()
    {
        RuleFor(request => request.ClosedByUserId)
            .NotEmpty()
            .Must(value => Guid.TryParse(value, out _))
            .WithMessage("ClosedByUserId must be a valid GUID.");
    }
}
