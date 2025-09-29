using System;
using System.Collections.Generic;

namespace BadmintonClub.Models;

public partial class Tournament
{
    public int TournamentId { get; set; }

    public string TenGiai { get; set; } = null!;

    public DateOnly NgayBatDau { get; set; }

    public DateOnly? NgayKetThuc { get; set; }

    public string Loai { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? GhiChu { get; set; }

    public virtual ICollection<EloHistory> EloHistories { get; set; } = new List<EloHistory>();

    public virtual ICollection<TournamentEvent> TournamentEvents { get; set; } = new List<TournamentEvent>();
}
