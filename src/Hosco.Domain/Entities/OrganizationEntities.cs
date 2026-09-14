using Hosco.Domain.Common;
using Hosco.Domain.Enums;

namespace Hosco.Domain.Entities;

public sealed class Tenant : Entity
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Branch> Branches { get; set; } = [];
}

public sealed class Branch : TenantEntity
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public Tenant Tenant { get; set; } = null!;
}

public sealed class AppUser : TenantEntity
{
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public required string PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<UserBranch> UserBranches { get; set; } = [];
}

public sealed class Role : Entity
{
    public SystemRole Name { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = [];
}

public sealed class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public AppUser User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}

public sealed class UserBranch
{
    public Guid UserId { get; set; }
    public Guid BranchId { get; set; }
    public AppUser User { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
}
