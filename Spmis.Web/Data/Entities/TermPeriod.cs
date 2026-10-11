using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class TermPeriod
{
    public uint Id { get; set; }

    public uint TermId { get; set; }

    public uint GradingPeriodId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public virtual GradingPeriod GradingPeriod { get; set; } = null!;

    public virtual AcademicTerm Term { get; set; } = null!;
}
