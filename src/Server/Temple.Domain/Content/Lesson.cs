namespace Temple.Domain.Content;

public class Lesson
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedUtc { get; set; }
}
