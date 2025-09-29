using System;
using System.Collections.Generic;

namespace BadmintonClub.Models;

public partial class EloHistory
{
    public int EloHistoryId { get; set; }

    public Guid MemberId { get; set; }

    public int TournamentId { get; set; }

    public int MatchId { get; set; }

    public int EloBefore { get; set; }

    public int EloAfter { get; set; }

    public int Delta { get; set; }

    public DateTime Timestamp { get; set; }

    public virtual Match Match { get; set; } = null!;

    public virtual Member Member { get; set; } = null!;

    public virtual Tournament Tournament { get; set; } = null!;
}
