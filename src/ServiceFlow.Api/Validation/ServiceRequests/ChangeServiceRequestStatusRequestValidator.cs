using FluentValidation;
using ServiceFlow.Api.Contracts.ServiceRequests;

namespace ServiceFlow.Api.Validation.ServiceRequests;

public sealed class ChangeServiceRequestStatusRequestValidator : AbstractValidator<ChangeServiceRequestStatusRequest>
{
    public ChangeServiceRequestStatusRequestValidator()
    {
        RuleFor(request => request.Status)
            .IsInEnum();

        RuleFor(request => request.ChangedByUserId)
            .NotEmpty();
    }
}
