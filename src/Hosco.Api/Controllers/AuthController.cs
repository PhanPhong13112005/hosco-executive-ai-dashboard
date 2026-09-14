using Hosco.Api.Security;
using Hosco.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hosco.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IIdentityStore identities, IPasswordVerifier passwords, JwtTokenService tokens) : ControllerBase
{
    public sealed record LoginRequest(string Email, string Password);

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await identities.FindByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);
        if (user is null || !user.IsActive || !passwords.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { code = "invalid_credentials", message = "Invalid email or password." });
        return Ok(tokens.Create(user));
    }
}
