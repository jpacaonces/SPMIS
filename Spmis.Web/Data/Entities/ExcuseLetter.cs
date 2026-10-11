using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class ExcuseLetter
{
    public uint Id { get; set; }

    public uint StudentId { get; set; }

    public string GuardianName { get; set; } = null!;

    public string GuardianRelationship { get; set; } = null!;

    public string SignatoryName { get; set; } = null!;

    public DateOnly DateFrom { get; set; }

    public DateOnly DateTo { get; set; }

    public string Reason { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public string OriginalFileName { get; set; } = null!;

    public DateTime SubmittedAt { get; set; }

    public virtual ICollection<ExcuseDecision> ExcuseDecisions { get; set; } = new List<ExcuseDecision>();

    public virtual Student Student { get; set; } = null!;
}
