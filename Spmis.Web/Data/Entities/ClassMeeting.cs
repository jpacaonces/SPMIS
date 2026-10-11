using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class ClassMeeting
{
    public uint Id { get; set; }

    public uint ClassOfferingId { get; set; }

    public DateOnly MeetingDate { get; set; }

    public string? Topic { get; set; }

    public uint? LinkedAssessmentId { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();

    public virtual ClassOffering ClassOffering { get; set; } = null!;

    public virtual Assessment? LinkedAssessment { get; set; }
}
