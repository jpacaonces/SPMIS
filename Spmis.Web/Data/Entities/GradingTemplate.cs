using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class GradingTemplate
{
    public uint Id { get; set; }

    public uint DepartmentId { get; set; }

    public string Name { get; set; } = null!;

    public decimal ClassStandingWeight { get; set; }

    public decimal MajorExamWeight { get; set; }

    public bool IsDefault { get; set; }

    public bool? IsActive { get; set; }

    public virtual Department Department { get; set; } = null!;

    public virtual ICollection<TemplateComponent> TemplateComponents { get; set; } = new List<TemplateComponent>();
}
