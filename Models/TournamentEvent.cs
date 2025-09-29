using System;
using System.Collections.Generic;

namespace BadmintonClub.Models;

public partial class TournamentEvent
{
    public int EventId { get; set; }

    public int TournamentId { get; set; }

    public string HangMuc { get; set; } = null!;

    public string QuyTacSeed { get; set; } = null!;

    public string? GhiChu { get; set; }

    public virtual ICollection<Match> Matches { get; set; } = new List<Match>();

    public virtual ICollection<Team> Teams { get; set; } = new List<Team>();

    public virtual Tournament Tournament { get; set; } = null!;
}
