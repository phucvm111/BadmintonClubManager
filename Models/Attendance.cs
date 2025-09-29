using System;
using System.Collections.Generic;

namespace BadmintonClub.Models;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public int SessionId { get; set; }

    public Guid MemberId { get; set; }

    public string Status { get; set; } = null!;

    public string? LyDoVang { get; set; }

    public string? GhiChu { get; set; }

    public virtual Member Member { get; set; } = null!;

    public virtual TrainingSession Session { get; set; } = null!;
}
