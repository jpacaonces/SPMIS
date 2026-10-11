using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class Section
{
    public uint Id { get; set; }

    public uint ProgramId { get; set; }

    public byte YearLevel { get; set; }

    public byte Semester { get; set; }

    public uint ShiftId { get; set; }

    public byte SectionNo { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<ClassOffering> ClassOfferings { get; set; } = new List<ClassOffering>();

    public virtual Program Program { get; set; } = null!;

    public virtual YearLevelShift YearLevelShift { get; set; } = null!;
}
