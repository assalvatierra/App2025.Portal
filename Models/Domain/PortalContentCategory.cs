using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Erp.Domain.Models;

public partial class PortalContentCategory
{
    public int Id { get; set; }

    public int? PortalCategoryId { get; set; }

    public int? PortalContentId { get; set; }

    [JsonIgnore]
    public virtual PortalContent? PortalContent { get; set; }
    [JsonIgnore]
    public virtual PortalCategory? PortalCategory { get; set; }

}
