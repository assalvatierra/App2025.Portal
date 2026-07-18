using System;
using System.Collections.Generic;

namespace Erp.Domain.Models;

public partial class PortalItem
{
    public int Id { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public string LastEditBy { get; set; } = null!;

    public DateTime LastEditOn { get; set; }

    public bool IsArchived { get; set; }

    public bool IsPrivate { get; set; }

    public bool IsActive { get; set; }

    public Guid? RecordGuid { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Remarks { get; set; }

    public string? Code { get; set; }

    public int? SortOrder { get; set; }

    public string? JsonData { get; set; }

    public int? ItemTypeId { get; set; }

    public int? ItemStatusId { get; set; }
    public virtual ICollection<PortalItemSpec> PortalItemSpecs { get; set; } = new List<PortalItemSpec>();
    public virtual ICollection<PortalItemCategory> PortalItemCategories { get; set; } = new List<PortalItemCategory>();
    public virtual ICollection<PortalItemPrice> PortalItemPrices { get; set; } = new List<PortalItemPrice>();
}
