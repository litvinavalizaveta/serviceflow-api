using FluentValidation;
using ServiceFlow.Api.Contracts.ServiceRequests;

namespace ServiceFlow.Api.Validation.ServiceRequests;

public sealed class AddRequestCommentRequestValidator : AbstractValidator<AddRequestCommentRequest>
{
    public AddRequestCommentRequestValidator()
    {
        RuleFor(request => request.Body)
            .NotEmpty()
            .MaximumLength(2_000);

        RuleFor(request => request.Visibility)
            .IsInEnum();
    }
}
