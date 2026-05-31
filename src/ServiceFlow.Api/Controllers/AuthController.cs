using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ServiceFlow.Api.Contracts.Auth;
using ServiceFlow.Api.Security;

namespace ServiceFlow.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private const string BearerTokenType = "Bearer";

    private readonly JwtOptions _jwtOptions;

    public AuthController(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    [HttpPost("demo-token")]
    [ProducesResponseType<DemoTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<DemoTokenResponse> CreateDemoToken(DemoTokenRequest request)
    {
        var expiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.ExpiresMinutes);
        var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? request.UserId
            : request.DisplayName.Trim();

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, request.UserId),
            new Claim("name", displayName),
            new Claim("role", request.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: credentials);

        return Ok(new DemoTokenResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            BearerTokenType,
            expiresAtUtc));
    }
}
