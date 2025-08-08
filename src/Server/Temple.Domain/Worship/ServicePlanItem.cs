namespace Temple.Domain.Worship;

public class ServicePlanItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ServicePlanId { get; set; }
    public int Order { get; set; }
    public string Type { get; set; } = "song"; // song, reading, announcement, prayer, custom
    public Guid? SongId { get; set; }
    public string? Key { get; set; }
    public string? Notes { get; set; }
}
