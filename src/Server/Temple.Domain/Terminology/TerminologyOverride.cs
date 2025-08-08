namespace Temple.Domain.Terminology;

public class TerminologyOverride
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string? TaxonomyId { get; set; }
    public string OverridesJson { get; set; } = "{}"; // key-value override map
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
