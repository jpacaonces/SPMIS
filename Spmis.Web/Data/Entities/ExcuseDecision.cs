using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class ExcuseDecision
{
    public uint Id { get; set; }

    public uint LetterId { get; set; }

    public uint ClassOfferingId { get; set; }

    public string Status { get; set; } = null!;

    public Guid? DecidedBy { get; set; }

    public DateTime? DecidedAt { get; set; }

    public string? DecisionReason { get; set; }

    public virtual ClassOffering ClassOffering { get; set; } = null!;

    public virtual User? DecidedByNavigation { get; set; }

    public virtual ExcuseLetter Letter { get; set; } = null!;
}
