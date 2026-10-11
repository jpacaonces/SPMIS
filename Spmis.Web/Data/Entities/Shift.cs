using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class Shift
{
    public uint Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual ICollection<YearLevelShift> YearLevelShifts { get; set; } = new List<YearLevelShift>();
}
