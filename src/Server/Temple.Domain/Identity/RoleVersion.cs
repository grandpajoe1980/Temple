namespace Temple.Domain.Identity;

public class RoleVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public int Version { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public string CapabilityHash { get; set; } = string.Empty; // hash of serialized role->capabilities map
}
