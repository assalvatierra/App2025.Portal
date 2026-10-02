using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Erp.Domain.Models;

public partial class PortalItemCategory
{
    public int Id { get; set; }

    public int? PortalItemId { get; set; }

    public int? PortalCategoryId { get; set; }

    [JsonIgnore]
    public virtual PortalCategory? PortalCategory { get; set; }
    [JsonIgnore]
    public virtual PortalItem? PortalItem { get; set; }
}
