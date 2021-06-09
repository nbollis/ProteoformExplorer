using Deconvoluter.ML;
using MassSpectrometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Deconvoluter
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting");

            //var file = @"C:\Users\rmillikin\Desktop\MetaMorpheus Problems\TDHyPRMSdata_forRMandJP\BR1\raw\032621_Lysate_tr2.raw";
            var file = @"C:\Users\rmillikin\Desktop\MetaMorpheus Problems\TDHyPRMSdata_forRMandJP\BR1\raw\032421_MALAT1Capture.raw";
            //var file = @"C:\Users\rmillikin\Desktop\MetaMorpheus Problems\TDHyPRMSdata_forRMandJP\BR1\raw\malat1_sliced.raw";

            var data = IO.ThermoRawFileReader.ThermoRawFileReader.LoadAllStaticData(file);

            var engine = new DeconvolutionEngine(2000, 0.4, 4, 0.4, 5, 5, 2, 60, 2);

            var envs = engine.Deconvolute(data, file).ToList();

            Console.WriteLine("Running ML");

            List<string> output = new List<string>();
            foreach (var env in envs)
            {
                var envelopeData = new EnvelopeData(env);
                output.Add(envelopeData.ToOutputString());
            }

            var classificationDataPath = @"C:\Users\rmillikin\Desktop\MetaMorpheus Problems\TDHyPRMSdata_forRMandJP\BR1\raw\classificationData_" +
                Path.GetFileNameWithoutExtension(file) + ".csv";
            var savedModelPath = @"C:\Users\rmillikin\Desktop\MetaMorpheus Problems\TDHyPRMSdata_forRMandJP\BR1\raw\trainedModel_" +
                Path.GetFileNameWithoutExtension(file) + ".zip";

            File.WriteAllLines(classificationDataPath, output);

            EnvelopeClassification classifier = new EnvelopeClassification(classificationDataPath, savedModelPath);
            classifier.TrainAndSavePredictor();

            foreach (var envelope in envs)
            {
                var envelopeData = new EnvelopeData(envelope);
                var prediction = classifier.Predict(envelopeData);
                envelope.MachineLearningClassification = prediction.PredictedClusterId.ToString();
            }

            Console.WriteLine("Writing output");

            output = new List<string>() { DeconvolutedEnvelope.TabDelimitedHeader };

            foreach (var env in envs)
            {
                output.Add(env.ToOutputString());
            }

            File.WriteAllLines(@"C:\Users\rmillikin\Desktop\MetaMorpheus Problems\TDHyPRMSdata_forRMandJP\BR1\raw\decon_" +
                Path.GetFileNameWithoutExtension(file) + ".tsv", output);

            Console.WriteLine("Done");
        }
    }
}
