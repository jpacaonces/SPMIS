using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class FinalGrade
{
    public uint Id { get; set; }

    public uint EnrollmentId { get; set; }

    public decimal RawGrade { get; set; }

    public decimal ReportedGrade { get; set; }

    public Guid FinalizedBy { get; set; }

    public DateTime FinalizedAt { get; set; }

    public virtual Enrollment Enrollment { get; set; } = null!;

    public virtual User FinalizedByNavigation { get; set; } = null!;
}
