using System;
using System.Collections.Generic;

namespace Erp.Domain.Models;

public partial class PortalItemPrice
{
    public int Id { get; set; }

    public int? PortalItemId { get; set; }

    public decimal BasePrice { get; set; }

    public string BaseCurrency { get; set; } = null!;

    public string BaseUnit { get; set; } = null!;

    public string? JsonData { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime ValidTo { get; set; }

    public string Status { get; set; } = null!;
}
