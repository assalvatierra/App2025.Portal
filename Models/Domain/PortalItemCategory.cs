using System;
using System.Collections.Generic;

namespace Erp.Domain.Models;

public partial class PortalItemCategory
{
    public int Id { get; set; }

    public int? PortalItemId { get; set; }

    public int? PortalCategoryId { get; set; }

    public virtual PortalCategory? PortalCategory { get; set; }
    public virtual PortalItem? PortalItem { get; set; }
}
