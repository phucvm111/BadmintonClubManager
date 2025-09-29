using System;
using System.Collections.Generic;

namespace BadmintonClub.Models;

public partial class AuditLog
{
    public long AuditId { get; set; }

    public string Actor { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string Entity { get; set; } = null!;

    public string EntityId { get; set; } = null!;

    public string? Payload { get; set; }

    public DateTime Timestamp { get; set; }
}
