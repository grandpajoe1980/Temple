namespace Temple.Domain.Tenants;

public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = "active"; // active, suspended, archived
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
