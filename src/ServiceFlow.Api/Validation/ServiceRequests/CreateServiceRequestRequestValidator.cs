using FluentValidation;
using ServiceFlow.Api.Contracts.ServiceRequests;
using ServiceFlow.Domain.ServiceRequests;

namespace ServiceFlow.Api.Validation.ServiceRequests;

public sealed class CreateServiceRequestRequestValidator : AbstractValidator<CreateServiceRequestRequest>
{
    public CreateServiceRequestRequestValidator()
    {
        RuleFor(request => request.ClientId)
            .NotEmpty();

        RuleFor(request => request.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.Description)
            .MaximumLength(ServiceRequest.DescriptionMaxLength);

        RuleFor(request => request.Priority)
            .IsInEnum();

        RuleFor(request => request.DueDateUtc)
            .GreaterThanOrEqualTo(_ => DateTimeOffset.UtcNow)
            .When(request => request.DueDateUtc is not null);
    }
}
