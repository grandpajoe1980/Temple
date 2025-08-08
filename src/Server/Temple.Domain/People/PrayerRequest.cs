namespace Temple.Domain.People;

public class PrayerRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PersonId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Confidentiality { get; set; } = "standard"; // standard, staff, pastors
    public DateTime? AnsweredUtc { get; set; }
    public Guid? AnsweredByUserId { get; set; }
}
