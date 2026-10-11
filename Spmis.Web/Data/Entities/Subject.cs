using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class Subject
{
    public uint Id { get; set; }

    public string Code { get; set; } = null!;

    public string Title { get; set; } = null!;

    public byte LectureUnits { get; set; }

    public byte LabUnits { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<ClassOffering> ClassOfferings { get; set; } = new List<ClassOffering>();

    public virtual ICollection<CurriculumSubject> CurriculumSubjects { get; set; } = new List<CurriculumSubject>();
}
