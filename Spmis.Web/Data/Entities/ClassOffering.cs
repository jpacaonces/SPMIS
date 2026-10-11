using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class ClassOffering
{
    public uint Id { get; set; }

    public uint TermId { get; set; }

    public uint SubjectId { get; set; }

    public uint SectionId { get; set; }

    public uint FacultyId { get; set; }

    public uint TransmutationTableId { get; set; }

    public decimal ClassStandingWeight { get; set; }

    public decimal MajorExamWeight { get; set; }

    public string AttendanceMode { get; set; } = null!;

    public decimal AbsenceLimitPercent { get; set; }

    public string Status { get; set; } = null!;

    public bool? IsActive { get; set; }

    public virtual ICollection<ClassComponent> ClassComponents { get; set; } = new List<ClassComponent>();

    public virtual ICollection<ClassMeeting> ClassMeetings { get; set; } = new List<ClassMeeting>();

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public virtual ICollection<ExcuseDecision> ExcuseDecisions { get; set; } = new List<ExcuseDecision>();

    public virtual Faculty Faculty { get; set; } = null!;

    public virtual Section Section { get; set; } = null!;

    public virtual Subject Subject { get; set; } = null!;

    public virtual AcademicTerm Term { get; set; } = null!;

    public virtual TransmutationTable TransmutationTable { get; set; } = null!;
}
