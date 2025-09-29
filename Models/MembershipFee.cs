using System;
using System.Collections.Generic;

namespace BadmintonClub.Models;

public partial class MembershipFee
{
    public int FeeId { get; set; }

    public Guid MemberId { get; set; }

    public string Period { get; set; } = null!;

    public decimal Amount { get; set; }

    public bool Paid { get; set; }

    public DateOnly? PaidDate { get; set; }

    public string? Note { get; set; }

    public virtual Member Member { get; set; } = null!;
}
