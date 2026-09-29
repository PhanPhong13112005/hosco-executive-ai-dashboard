using Hosco.Application.Abstractions;
using Hosco.Application.Services;
using Hosco.Domain.Enums;

namespace Hosco.UnitTests;

public sealed class BranchScopeTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly Guid Allowed = Guid.NewGuid();

    [Fact]
    public async Task Branch_manager_can_access_assigned_branch()
    {
        var validator = new BranchScopeValidator(User(SystemRole.BranchManager, [Allowed]), new Directory(Tenant, new HashSet<Guid> { Allowed }));
        await validator.EnsureCanAccessAsync(Allowed, default);
    }

    [Fact]
    public async Task Branch_manager_cannot_access_unassigned_branch()
    {
        var other = Guid.NewGuid();
        var validator = new BranchScopeValidator(User(SystemRole.BranchManager, [Allowed]), new Directory(Tenant, new HashSet<Guid> { Allowed, other }));
        await Assert.ThrowsAsync<ForbiddenException>(() => validator.EnsureCanAccessAsync(other, default));
    }

    [Fact]
    public async Task Chain_manager_can_access_any_branch_in_same_tenant()
    {
        var other = Guid.NewGuid();
        var validator = new BranchScopeValidator(User(SystemRole.ChainManager, []), new Directory(Tenant, new HashSet<Guid> { other }));
        await validator.EnsureCanAccessAsync(other, default);
    }

    [Theory]
    [InlineData(SystemRole.Owner)]
    [InlineData(SystemRole.SystemAdmin)]
    public async Task Owner_and_system_admin_require_explicit_branch_assignment(SystemRole role)
    {
        var other = Guid.NewGuid();
        var validator = new BranchScopeValidator(User(role, [Allowed]), new Directory(Tenant, new HashSet<Guid> { Allowed, other }));
        await Assert.ThrowsAsync<ForbiddenException>(() => validator.EnsureCanAccessAsync(other, default));
    }

    [Fact]
    public async Task Cross_tenant_branch_is_always_forbidden()
    {
        var other = Guid.NewGuid();
        var validator = new BranchScopeValidator(User(SystemRole.Owner, []), new Directory(Guid.NewGuid(), new HashSet<Guid> { other }));
        await Assert.ThrowsAsync<ForbiddenException>(() => validator.EnsureCanAccessAsync(other, default));
    }

    private static ICurrentUser User(SystemRole role, Guid[] branches) => new FakeUser(Tenant, new HashSet<SystemRole> { role }, branches.ToHashSet());

    private sealed record FakeUser(Guid TenantId, IReadOnlySet<SystemRole> Roles, IReadOnlySet<Guid> BranchIds) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid UserId => Guid.NewGuid();
    }

    private sealed record Directory(Guid TenantId, IReadOnlySet<Guid> Branches) : IBranchDirectory
    {
        public Task<bool> BelongsToTenantAsync(Guid branchId, Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult(tenantId == TenantId && Branches.Contains(branchId));
    }
}
