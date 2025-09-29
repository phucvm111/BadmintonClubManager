// Services/ML/AbsenceSample.cs
using Microsoft.ML.Data;

namespace BadmintonClub.Services.ML;

public sealed class AbsenceSample
{
    // Numeric features
    public float AttendanceRate4 { get; set; }
    public float AttendanceRate8 { get; set; }
    public float DaysSincePresent { get; set; }
    public float HourSlot { get; set; }

    // Categorical (one-hot)
    public string DayOfWeek { get; set; } = "";   // Mon..Sun
    public string GroupLevel { get; set; } = "";  // Cao/TrungBinh/Thap

    // Label: 1=Absent, 0=Present/Late
    public bool Label { get; set; }
}

