using System;
using System.Collections.Generic;

namespace BadmintonClub.Models;

public partial class Team
{
    public int TeamId { get; set; }

    public int EventId { get; set; }

    public string? TenDoi { get; set; }

    public virtual TournamentEvent Event { get; set; } = null!;

    public virtual ICollection<Match> MatchTeamAs { get; set; } = new List<Match>();

    public virtual ICollection<Match> MatchTeamBs { get; set; } = new List<Match>();

    public virtual ICollection<Member> Members { get; set; } = new List<Member>();
}
