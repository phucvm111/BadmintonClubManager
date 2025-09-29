// Services/ML/AbsenceTrainer.cs
using Microsoft.ML;
using Microsoft.ML.Data;
using System.IO;

namespace BadmintonClub.Services.ML;

public sealed class AbsenceTrainer
{
    private readonly MLContext _ml = new(seed: 42);

    public (ITransformer model, MulticlassClassificationMetrics? multi, BinaryClassificationMetrics? binary) Train(List<AbsenceSample> data)
    {
        var trainData = _ml.Data.LoadFromEnumerable(data);

        // Tiền xử lý: one-hot DayOfWeek/GroupLevel, concat features, normalize
        var pipeline =
            _ml.Transforms.Categorical.OneHotEncoding(new[]
            {
                new InputOutputColumnPair("DayOfWeekEncoded", "DayOfWeek"),
                new InputOutputColumnPair("GroupLevelEncoded", "GroupLevel")
            })
            .Append(_ml.Transforms.Concatenate("Features",
                "AttendanceRate4", "AttendanceRate8", "DaysSincePresent", "HourSlot", "DayOfWeekEncoded", "GroupLevelEncoded"))
            .Append(_ml.Transforms.NormalizeMinMax("Features"))
            // Thuật toán nhị phân: SDCA Logistic Regression
            .Append(_ml.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: "Label", featureColumnName: "Features"));

        var split = _ml.Data.TrainTestSplit(trainData, testFraction: 0.2);
        var model = pipeline.Fit(split.TrainSet);

        var predictions = model.Transform(split.TestSet);
        var metrics = _ml.BinaryClassification.Evaluate(predictions, labelColumnName: "Label");

        return (model, null, metrics);
    }

    public void Save(ITransformer model, DataViewSchema schema, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _ml.Model.Save(model, schema, path);
    }

    public ITransformer Load(string path) => _ml.Model.Load(path, out _);
}
