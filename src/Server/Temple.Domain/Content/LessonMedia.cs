namespace Temple.Domain.Content;

public class LessonMedia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid LessonId { get; set; }
    public Guid MediaAssetId { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
