using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class ActivityLog
{
    public ulong Id { get; set; }

    public DateTime OccurredAt { get; set; }

    public Guid? UserId { get; set; }

    public string? Role { get; set; }

    public string ActionType { get; set; } = null!;

    public string EntityType { get; set; } = null!;

    public string? EntityId { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? Reason { get; set; }

    public virtual User? User { get; set; }
}
