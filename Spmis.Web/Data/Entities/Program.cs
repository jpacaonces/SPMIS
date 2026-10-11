using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class Program
{
    public uint Id { get; set; }

    public uint DepartmentId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public bool? IsActive { get; set; }

    public virtual ICollection<CurriculumSubject> CurriculumSubjects { get; set; } = new List<CurriculumSubject>();

    public virtual Department Department { get; set; } = null!;

    public virtual ICollection<ProgramHead> ProgramHeads { get; set; } = new List<ProgramHead>();

    public virtual ICollection<Section> Sections { get; set; } = new List<Section>();

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}
