using System;
using System.Collections.Generic;

namespace BadmintonClub.Models;

public partial class TrainingSession
{
    public int SessionId { get; set; }

    public string TenBuoi { get; set; } = null!;

    public DateOnly Ngay { get; set; }

    public TimeOnly GioBatDau { get; set; }

    public TimeOnly GioKetThuc { get; set; }

    public string NhomTrinhDo { get; set; } = null!;

    public string? GhiChu { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}
