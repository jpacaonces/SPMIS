using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class SubmissionCheck
{
    public uint Id { get; set; }

    public uint ScoreId { get; set; }

    public decimal SimilarityPct { get; set; }

    public Guid CheckedBy { get; set; }

    public DateTime CheckedAt { get; set; }

    public string? Note { get; set; }

    public virtual User CheckedByNavigation { get; set; } = null!;

    public virtual Score Score { get; set; } = null!;
}
