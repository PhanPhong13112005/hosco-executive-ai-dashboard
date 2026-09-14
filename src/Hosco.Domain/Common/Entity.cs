namespace Hosco.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public abstract class TenantEntity : Entity
{
    public Guid TenantId { get; set; }
}
