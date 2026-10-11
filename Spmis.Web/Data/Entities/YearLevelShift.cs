using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class YearLevelShift
{
    public byte YearLevel { get; set; }

    public uint ShiftId { get; set; }

    public virtual ICollection<Section> Sections { get; set; } = new List<Section>();

    public virtual Shift Shift { get; set; } = null!;
}
