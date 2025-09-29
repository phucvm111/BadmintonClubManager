// Services/ML/AbsencePredictor.cs
using Microsoft.ML;

namespace BadmintonClub.Services.ML;

public sealed class AbsencePredictor
{
    private readonly MLContext _ml = new();

    private ITransformer _model;
    private PredictionEngine<AbsenceSample, AbsencePrediction> _engine;

    public AbsencePredictor(string modelPath)
    {
        _model = _ml.Model.Load(modelPath, out _);
        _engine = _ml.Model.CreatePredictionEngine<AbsenceSample, AbsencePrediction>(_model);
    }

    public AbsencePrediction Predict(AbsenceSample sample) => _engine.Predict(sample);
}
