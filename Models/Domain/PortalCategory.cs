using System;
using System.Collections.Generic;

namespace Erp.Domain.Models;

public partial class PortalCategory
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? JsonData { get; set; }

    public string? Status { get; set; }
    public int? OrderSeq { get; set; }
    public string? CategoryType { get; set; }

    public virtual ICollection<PortalItemCategory> PortalItemCategories { get; set; } = new List<PortalItemCategory>();
}
