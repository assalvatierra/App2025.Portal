using System;
using System.Collections.Generic;

namespace Erp.Domain.Models;

public partial class PortalReservation
{
    public int Id { get; set; }

    public string TransactionType { get; set; } = null!;

    public int? PortalItemId { get; set; }

    public string CustomerName { get; set; } = null!;

    public string? ContactNo { get; set; }

    public string? ContactEmail { get; set; }

    public DateTime DateReceived { get; set; }

    public string JsonData { get; set; } = null!;

    public string Status { get; set; } = null!;

}
