using System;
using System.Collections.Generic;

namespace Erp.Domain.Models;

public partial class PortalContentData
{
    public int Id { get; set; }

    public string DataType { get; set; } = null!;

    public string? DataValue { get; set; } = null!;

    public string? SeoData { get; set; } = null;
}
