namespace Temple.Domain.People;

public class PastoralCareNote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PersonId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public string Sensitivity { get; set; } = "standard"; // standard, confidential
    public string Note { get; set; } = string.Empty;
}
