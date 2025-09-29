using System;
using System.Collections.Generic;

namespace BadmintonClub.Models;

public partial class FinanceCategory
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<FinanceEntry> FinanceEntries { get; set; } = new List<FinanceEntry>();
}
