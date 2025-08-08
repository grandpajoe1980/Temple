namespace Temple.Domain.Groups;

public class GroupMeetingAttendance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid GroupId { get; set; }
    public Guid MeetingId { get; set; }
    public Guid PersonId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
