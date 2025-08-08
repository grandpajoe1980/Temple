namespace Temple.Domain.Groups;

public class GroupMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid GroupId { get; set; }
    public Guid PersonId { get; set; }
    public string Role { get; set; } = "member"; // member, leader, assistant
    public DateTime JoinedUtc { get; set; } = DateTime.UtcNow;
}
