using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class ClassComponent
{
    public uint Id { get; set; }

    public uint ClassOfferingId { get; set; }

    public string ComponentType { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? GroupType { get; set; }

    public decimal WeightPct { get; set; }

    public bool IsAttendance { get; set; }

    public byte SortOrder { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();

    public virtual ClassOffering ClassOffering { get; set; } = null!;
}
