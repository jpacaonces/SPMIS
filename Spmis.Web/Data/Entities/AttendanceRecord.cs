using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class AttendanceRecord
{
    public uint Id { get; set; }

    public uint MeetingId { get; set; }

    public uint EnrollmentId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime RowVersion { get; set; }

    public virtual Enrollment Enrollment { get; set; } = null!;

    public virtual ClassMeeting Meeting { get; set; } = null!;
}
