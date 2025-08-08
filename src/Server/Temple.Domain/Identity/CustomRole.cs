namespace Temple.Domain.Identity;

public class CustomRole
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Key { get; set; } = string.Empty; // unique per tenant
    public string Name { get; set; } = string.Empty; // display label
    public string[] Capabilities { get; set; } = Array.Empty<string>();
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }
    public bool System { get; set; } // built-in lock
}
