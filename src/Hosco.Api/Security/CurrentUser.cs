using System.Security.Claims;
using Hosco.Application.Abstractions;
using Hosco.Domain.Enums;

namespace Hosco.Api.Security;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal Principal => accessor.HttpContext?.User ?? new ClaimsPrincipal();
    public bool IsAuthenticated => Principal.Identity?.IsAuthenticated == true;
    public Guid UserId => ParseRequired(ClaimTypes.NameIdentifier, "sub");
    public Guid TenantId => ParseRequired("tenant_id");
    public IReadOnlySet<SystemRole> Roles => Principal.FindAll(ClaimTypes.Role).Select(x => Enum.Parse<SystemRole>(x.Value)).ToHashSet();
    public IReadOnlySet<Guid> BranchIds => Principal.FindAll("branch_id").Select(x => Guid.Parse(x.Value)).ToHashSet();

    private Guid ParseRequired(params string[] types)
    {
        foreach (var type in types)
            if (Guid.TryParse(Principal.FindFirstValue(type), out var value)) return value;
        throw new InvalidOperationException($"Authenticated user is missing required claim: {string.Join("/", types)}.");
    }
}
