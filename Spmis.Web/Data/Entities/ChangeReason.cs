using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class ChangeReason
{
    public uint Id { get; set; }

    public string Context { get; set; } = null!;

    public string Text { get; set; } = null!;

    public bool? IsActive { get; set; }
}
