namespace Temple.Domain.People;

public class AttendanceRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PersonId { get; set; }
    public Guid? ScheduleEventId { get; set; }
    public DateTime DateUtc { get; set; } = DateTime.UtcNow;
    public string? Source { get; set; } // manual, import, scan
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
