using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class TransmutationRow
{
    public uint Id { get; set; }

    public uint TableId { get; set; }

    public decimal MinRaw { get; set; }

    public decimal ReportedGrade { get; set; }

    public bool IsPassing { get; set; }

    public virtual TransmutationTable Table { get; set; } = null!;
}
