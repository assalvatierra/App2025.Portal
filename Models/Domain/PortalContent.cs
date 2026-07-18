using System;
using System.Collections.Generic;

namespace Erp.Domain.Models;

public partial class PortalContent
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? JsonData { get; set; }

    public string Status { get; set; } = null!;

    public int OrderSeq { get; set; }

    public virtual ICollection<PortalContentCategory> PortalContentCategories { get; set; } = new List<PortalContentCategory>();
}
