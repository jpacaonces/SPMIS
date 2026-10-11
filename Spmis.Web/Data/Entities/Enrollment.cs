using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class Enrollment
{
    public uint Id { get; set; }

    public uint StudentId { get; set; }

    public uint ClassOfferingId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime EnrolledAt { get; set; }

    public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();

    public virtual ClassOffering ClassOffering { get; set; } = null!;

    public virtual FinalGrade? FinalGrade { get; set; }

    public virtual ICollection<Score> Scores { get; set; } = new List<Score>();

    public virtual Student Student { get; set; } = null!;
}
