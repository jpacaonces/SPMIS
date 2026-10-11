using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class Department
{
    public uint Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public bool? IsActive { get; set; }

    public virtual ICollection<Faculty> Faculties { get; set; } = new List<Faculty>();

    public virtual ICollection<GradingTemplate> GradingTemplates { get; set; } = new List<GradingTemplate>();

    public virtual ICollection<Program> Programs { get; set; } = new List<Program>();
}
