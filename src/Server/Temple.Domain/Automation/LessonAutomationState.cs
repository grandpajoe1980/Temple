namespace Temple.Domain.Automation;

// Tracks current automated lesson selection and manual override state per tenant
public class LessonAutomationState
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? ActiveLessonId { get; set; }
    public bool ManualOverride { get; set; } // if true, rotation job will skip changing ActiveLessonId
    public DateTime? OverrideSetUtc { get; set; }
    public DateTime? LastRotationUtc { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
