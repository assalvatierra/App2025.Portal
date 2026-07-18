using System;
using System.Collections.Generic;

namespace Erp.Domain.Models;

public partial class PortalItemSpec
{
    public int Id { get; set; }

    public int? PortalItemId { get; set; }

    public string? JsonData { get; set; }

    public int Order { get; set; }

    public string? Remarks { get; set; }

    public virtual PortalItem? PortalItem { get; set; }
}
