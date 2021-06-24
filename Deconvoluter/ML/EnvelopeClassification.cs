using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Deconvoluter.ML
{
    public class EnvelopeClassification
    {

        public static string pathToTrainingFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"TrainingAndTestingData");
        public static string pathToModelFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"TrainedModels");
        private IDataView _trainingDataView;
        private PredictionEngine<EnvelopeData, ClassificationPrediction> _predictionEngine;
        private ITransformer _trainedModel;
        private MLContext _mlContext;

        public EnvelopeClassification()
        {
            _mlContext = new MLContext(seed: 0);
        }

        public void TrainAndTestModel(string trainingDataPath, string testingDataPath)
        {
            // load training data
            _trainingDataView = _mlContext.Data.LoadFromTextFile<EnvelopeData>(trainingDataPath, hasHeader: true, separatorChar: ',');

            // train model
            BuildAndTrainModel(_trainingDataView);

            // load test data
            var _testingDataView = _mlContext.Data.LoadFromTextFile<EnvelopeData>(testingDataPath, hasHeader: true, separatorChar: ',');

            // evaluate model
            EvaluateModel(_testingDataView);
        }

        public void SaveModel(string path)
        {
            _mlContext.Model.Save(_trainedModel, _trainingDataView.Schema, path);
        }

        public void LoadModel(string path)
        {
            _trainedModel = _mlContext.Model.Load(path, out var trainingDataSchema);
        }

        public ClassificationPrediction Classify(EnvelopeData envelope)
        {
            if (_predictionEngine == null)
            {
                throw new Exception("Model must be loaded or trained prior to use");
            }

            var classification = _predictionEngine.Predict(envelope);

            return classification;
        }

        public List<string> GetModelPredictedLabelNames(string name)
        {
            var column = _trainingDataView.Schema.GetColumnOrNull(name);

            var slotNames = new VBuffer<ReadOnlyMemory<char>>();
            column.Value.GetSlotNames(ref slotNames);
            var names = new string[slotNames.Length];
            var num = 0;
            foreach (var denseValue in slotNames.DenseValues())
            {
                names[num++] = denseValue.ToString();
            }

            return names.ToList();
        }

        private IEstimator<ITransformer> BuildAndTrainModel(IDataView trainingDataView)
        {
            var featureNames = typeof(EnvelopeData).GetFields().Select(p => p.Name).ToList();
            featureNames.Remove("Label");
            var arrayOfFeatures = featureNames.ToArray();

            var dataProcessPipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label", "Label")
                                      .Append(_mlContext.Transforms.Concatenate("Features", arrayOfFeatures))
                                      .AppendCacheCheckpoint(_mlContext);

            var trainer = _mlContext.MulticlassClassification.Trainers
                //.SdcaMaximumEntropy(labelColumnName: "Label", featureColumnName: "Features")
                .LightGbm(labelColumnName: "Label", featureColumnName: "Features")
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel", "PredictedLabel"));

            var trainingPipeline = dataProcessPipeline.Append(trainer);

            _trainedModel = trainingPipeline.Fit(trainingDataView);

            _predictionEngine = _mlContext.Model.CreatePredictionEngine<EnvelopeData, ClassificationPrediction>(_trainedModel);

            return trainingPipeline;
        }

        private void EvaluateModel(IDataView trainingDataView)
        {
            var testMetrics = _mlContext.MulticlassClassification.Evaluate(_trainedModel.Transform(trainingDataView));

            Console.WriteLine($"=============== Evaluating to get model's accuracy metrics - Ending time: {DateTime.Now.ToString()} ===============");
            Console.WriteLine($"*************************************************************************************************************");
            Console.WriteLine($"*       Metrics for Multi-class Classification model - Test Data     ");
            Console.WriteLine($"*------------------------------------------------------------------------------------------------------------");
            Console.WriteLine($"*       MicroAccuracy:    {testMetrics.MicroAccuracy:0.###}");
            Console.WriteLine($"*       MacroAccuracy:    {testMetrics.MacroAccuracy:0.###}");
            Console.WriteLine($"*       LogLoss:          {testMetrics.LogLoss:#.###}");
            Console.WriteLine($"*       LogLossReduction: {testMetrics.LogLossReduction:#.###}");
            Console.WriteLine($"*************************************************************************************************************");
        }
    }
}
