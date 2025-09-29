using System;
using System.Collections.Generic;

namespace BadmintonClub.Models;

public partial class FinanceEntry
{
    public int EntryId { get; set; }

    public DateOnly Date { get; set; }

    public Guid? MemberId { get; set; }

    public int CategoryId { get; set; }

    public decimal Amount { get; set; }

    public string? Description { get; set; }

    public DateOnly? DueDate { get; set; }

    public bool IsOverdue { get; set; }

    public bool NotifiedEmail { get; set; }

    public int? TournamentId { get; set; }

    public virtual FinanceCategory Category { get; set; } = null!;

    public virtual Member? Member { get; set; }
}
