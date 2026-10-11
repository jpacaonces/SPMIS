using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class Faculty
{
    public uint Id { get; set; }

    public Guid UserId { get; set; }

    public string EmployeeNo { get; set; } = null!;

    public uint DepartmentId { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<ClassOffering> ClassOfferings { get; set; } = new List<ClassOffering>();

    public virtual Department Department { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
