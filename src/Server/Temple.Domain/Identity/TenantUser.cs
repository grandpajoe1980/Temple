namespace Temple.Domain.Identity;

public class TenantUser
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string RoleKey { get; set; } = string.Empty; // e.g., tenant_owner, leader, contributor, member, guest
    public string? CustomRoleLabel { get; set; }
    public string? CapabilitiesJson { get; set; } // JSON override list if needed
    public DateTime JoinedUtc { get; set; } = DateTime.UtcNow;
}
