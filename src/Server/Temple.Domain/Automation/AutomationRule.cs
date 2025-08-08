namespace Temple.Domain.Automation;

public class AutomationRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string TriggerType { get; set; } = string.Empty; // e.g., NewMemberJoined
    public string ConditionJson { get; set; } = string.Empty; // serialized condition
    public string ActionJson { get; set; } = string.Empty; // serialized action(s)
    public bool Enabled { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
