using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class ProgramHead
{
    public uint Id { get; set; }

    public Guid UserId { get; set; }

    public uint ProgramId { get; set; }

    public bool? IsActive { get; set; }

    public virtual Program Program { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
