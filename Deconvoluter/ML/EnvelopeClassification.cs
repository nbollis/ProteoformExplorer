using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Deconvoluter.ML
{
    public class EnvelopeClassification
    {
        readonly string _dataPath;
        readonly string _modelPath;
        PredictionEngine<EnvelopeData, ClusterPrediction> Predictor;
        MLContext mlContext;

        public EnvelopeClassification(string dataPath, string modelPath)
        {
            _dataPath = dataPath;
            _modelPath = modelPath;
            mlContext = new MLContext(seed: 0);
        }

        public void TrainAndSavePredictor()
        {
            var typeProperties = typeof(EnvelopeData).GetFields().Select(p => p.Name).ToArray();

            var dataView = mlContext.Data.LoadFromTextFile<EnvelopeData>(_dataPath, hasHeader: false, separatorChar: ',');

            string featuresColumnName = "Features";

            var pipeline = mlContext.Transforms
                .Concatenate(featuresColumnName, typeProperties)
                .Append(mlContext.Clustering.Trainers.KMeans(featuresColumnName, numberOfClusters: 4));

            var model = pipeline.Fit(dataView);

            using (var fileStream = new FileStream(_modelPath, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                mlContext.Model.Save(model, dataView.Schema, fileStream);
            }

            Predictor = mlContext.Model.CreatePredictionEngine<EnvelopeData, ClusterPrediction>(model);
        }

        public void LoadModel()
        {
            ITransformer model;

            using (var fileStream = new FileStream(_modelPath, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                model = mlContext.Model.Load(fileStream, out var inputSchema);
            }

            Predictor = mlContext.Model.CreatePredictionEngine<EnvelopeData, ClusterPrediction>(model);
        }

        public ClusterPrediction Predict(EnvelopeData envelope)
        {
            if (Predictor == null)
            {
                throw new Exception("Predictor must be loaded or trained prior to use");
            }

            var prediction = Predictor.Predict(envelope);

            return prediction;
        }
    }
}
