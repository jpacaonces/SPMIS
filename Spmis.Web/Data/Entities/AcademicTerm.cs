using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class AcademicTerm
{
    public uint Id { get; set; }

    public string SchoolYear { get; set; } = null!;

    public byte Semester { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public bool IsCurrent { get; set; }

    public virtual ICollection<ClassOffering> ClassOfferings { get; set; } = new List<ClassOffering>();

    public virtual ICollection<TermPeriod> TermPeriods { get; set; } = new List<TermPeriod>();
}
