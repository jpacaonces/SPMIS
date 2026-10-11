using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class TemplateComponent
{
    public uint Id { get; set; }

    public uint TemplateId { get; set; }

    public string ComponentType { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? GroupType { get; set; }

    public decimal WeightPct { get; set; }

    public bool IsAttendance { get; set; }

    public byte SortOrder { get; set; }

    public bool? IsActive { get; set; }

    public virtual GradingTemplate Template { get; set; } = null!;
}
