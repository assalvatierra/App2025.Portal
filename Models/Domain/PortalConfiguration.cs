using System;
using System.Collections.Generic;

namespace Erp.Domain.Models;

public partial class PortalConfiguration
{
    public short Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsEnabled { get; set; }

    public DateTime Expiry { get; set; }

    public string SysCode { get; set; } = null!;

    public string? Settings { get; set; }
}
