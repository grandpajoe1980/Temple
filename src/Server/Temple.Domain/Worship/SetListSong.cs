namespace Temple.Domain.Worship;

public class SetListSong
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid SetListId { get; set; }
    public Guid SongId { get; set; }
    public int Order { get; set; }
    public string? Key { get; set; }
}
