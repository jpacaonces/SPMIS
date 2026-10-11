using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class GradingPeriod
{
    public uint Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public byte SortOrder { get; set; }

    public virtual ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();

    public virtual ICollection<TermPeriod> TermPeriods { get; set; } = new List<TermPeriod>();
}
