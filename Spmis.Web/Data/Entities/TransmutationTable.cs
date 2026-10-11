using System;
using System.Collections.Generic;

namespace Spmis.Web.Data.Entities;

public partial class TransmutationTable
{
    public uint Id { get; set; }

    public string Name { get; set; } = null!;

    public bool? IsSample { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<ClassOffering> ClassOfferings { get; set; } = new List<ClassOffering>();

    public virtual ICollection<TransmutationRow> TransmutationRows { get; set; } = new List<TransmutationRow>();
}
