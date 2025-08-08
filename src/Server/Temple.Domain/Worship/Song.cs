namespace Temple.Domain.Worship;

public class Song
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? CcliNumber { get; set; }
    public string? DefaultKey { get; set; }
    public string? ArrangementNotes { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }
}
