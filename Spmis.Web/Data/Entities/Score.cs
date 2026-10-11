using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class Score
{
    public uint Id { get; set; }

    public uint AssessmentId { get; set; }

    public uint EnrollmentId { get; set; }

    public decimal? RawScore { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? SubmittedAt { get; set; }

    public DateTime RowVersion { get; set; }

    public virtual Assessment Assessment { get; set; } = null!;

    public virtual Enrollment Enrollment { get; set; } = null!;

    public virtual ICollection<SubmissionCheck> SubmissionChecks { get; set; } = new List<SubmissionCheck>();
}
