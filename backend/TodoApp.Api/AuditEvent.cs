/// <summary>One record in the audit trail: who did what, to which item, when, and whether it worked.</summary>
public class AuditEvent
{
    public long Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? UserId { get; set; }
    public string Action { get; set; } = "";
    public string? TargetType { get; set; }
    public string? TargetId { get; set; }
    public string? Changes { get; set; }
    public string Outcome { get; set; } = "";
    public string? TraceId { get; set; }
    public string? IpAddress { get; set; }
}
