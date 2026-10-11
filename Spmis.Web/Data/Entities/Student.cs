using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class Student
{
    public uint Id { get; set; }

    public Guid UserId { get; set; }

    public string StudentNo { get; set; } = null!;

    public uint ProgramId { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public virtual ICollection<ExcuseLetter> ExcuseLetters { get; set; } = new List<ExcuseLetter>();

    public virtual Program Program { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
