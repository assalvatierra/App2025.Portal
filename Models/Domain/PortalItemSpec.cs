using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Erp.Domain.Models;

public partial class PortalItemSpec
{
    public int Id { get; set; }

    public int? PortalItemId { get; set; }

    public string? JsonData { get; set; }

    public int Order { get; set; }

    public string? Remarks { get; set; }

    [JsonIgnore]
    public virtual PortalItem? PortalItem { get; set; }
}
