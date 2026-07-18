using System;
using System.Collections.Generic;

namespace Erp.Domain.Models;

public partial class PortalContentCategory
{
    public int Id { get; set; }

    public int? PortalCategoryId { get; set; }

    public int? PortalContentId { get; set; }

    public virtual PortalContent? PortalContent { get; set; }
    public virtual PortalCategory? PortalCategory { get; set; }

}
