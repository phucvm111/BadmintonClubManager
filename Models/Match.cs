using System;
using System.Collections.Generic;

namespace BadmintonClub.Models;

public partial class Match
{
    public int MatchId { get; set; }

    public int EventId { get; set; }

    public int Round { get; set; }

    public int TeamAid { get; set; }

    public int TeamBid { get; set; }

    public int? WinnerTeamId { get; set; }

    public string? ScoreJson { get; set; }

    public bool Completed { get; set; }

    public DateTime? StartTime { get; set; }

    public string? CourtNo { get; set; }

    public int? PrevMatchAid { get; set; }

    public int? PrevMatchBid { get; set; }

    public virtual ICollection<EloHistory> EloHistories { get; set; } = new List<EloHistory>();

    public virtual TournamentEvent Event { get; set; } = null!;

    public virtual ICollection<Match> InversePrevMatchA { get; set; } = new List<Match>();

    public virtual ICollection<Match> InversePrevMatchB { get; set; } = new List<Match>();

    public virtual Match? PrevMatchA { get; set; }

    public virtual Match? PrevMatchB { get; set; }

    public virtual Team TeamA { get; set; } = null!;

    public virtual Team TeamB { get; set; } = null!;
}
