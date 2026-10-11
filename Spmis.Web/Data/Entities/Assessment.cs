using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class Assessment
{
    public uint Id { get; set; }

    public uint ClassComponentId { get; set; }

    public uint GradingPeriodId { get; set; }

    public string Title { get; set; } = null!;

    public decimal MaxScore { get; set; }

    public DateTime? DueDate { get; set; }

    public decimal PassingPercent { get; set; }

    public bool? IsRequired { get; set; }

    public bool? IsActive { get; set; }

    public virtual ClassComponent ClassComponent { get; set; } = null!;

    public virtual ICollection<ClassMeeting> ClassMeetings { get; set; } = new List<ClassMeeting>();

    public virtual GradingPeriod GradingPeriod { get; set; } = null!;

    public virtual ICollection<Score> Scores { get; set; } = new List<Score>();
}
