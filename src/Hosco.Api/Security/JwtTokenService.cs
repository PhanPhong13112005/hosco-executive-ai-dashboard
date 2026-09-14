using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Hosco.Application.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace Hosco.Api.Security;

public sealed class JwtOptions
{
    public const string Section = "Jwt";
    public string Issuer { get; set; } = "Hosco.Api";
    public string Audience { get; set; } = "Hosco.Clients";
    public string SigningKey { get; set; } = "";
    public int ExpiryMinutes { get; set; } = 60;
}

public sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiresAt, string TokenType = "Bearer");

public sealed class JwtTokenService(JwtOptions options)
{
    public TokenResponse Create(IdentityRecord user)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(options.ExpiryMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new("tenant_id", user.TenantId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(user.Roles.Select(x => new Claim(ClaimTypes.Role, x.ToString())));
        claims.AddRange(user.BranchIds.Select(x => new Claim("branch_id", x.ToString())));
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(options.Issuer, options.Audience, claims, now.UtcDateTime, expires.UtcDateTime, credentials);
        return new TokenResponse(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
