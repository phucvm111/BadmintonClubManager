using Microsoft.ML.Data;

namespace BadmintonClub.Services.ML;

public sealed class AbsencePrediction
{
    [ColumnName("PredictedLabel")]
    public bool PredictedAbsent { get; set; }

    public float Probability { get; set; }
    public float Score { get; set; }
}