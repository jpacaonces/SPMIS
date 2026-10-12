namespace Spmis.Web.Models.Admin;

public class AuditLogItem
{
    public DateTime? OccurredAt { get; set; }
    public string UserLabel { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? Reason { get; set; }
}