using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ServiceFlow.Domain.Common;

namespace ServiceFlow.Api.Security;

public static class CurrentUserExtensions
{
    public static Guid GetRequiredSubjectGuid(this ClaimsPrincipal user)
    {
        var subject = user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new DomainException("Authenticated user id claim is required.");
        }

        if (!Guid.TryParse(subject, out var userId))
        {
            throw new DomainException("Authenticated user id must be a valid GUID.");
        }

        return userId;
    }

    public static bool CanSeeInternalComments(this ClaimsPrincipal user)
    {
        return user.IsInRole(ServiceFlowRoles.Admin) || user.IsInRole(ServiceFlowRoles.Agent);
    }
}
