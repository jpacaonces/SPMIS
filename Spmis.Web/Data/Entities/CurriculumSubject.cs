using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class CurriculumSubject
{
    public uint Id { get; set; }

    public uint ProgramId { get; set; }

    public byte YearLevel { get; set; }

    public byte Semester { get; set; }

    public uint SubjectId { get; set; }

    public bool? IsActive { get; set; }

    public virtual Program Program { get; set; } = null!;

    public virtual Subject Subject { get; set; } = null!;
}
