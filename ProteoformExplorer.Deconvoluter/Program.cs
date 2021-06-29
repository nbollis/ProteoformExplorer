using ProteoformExplorer.Deconvoluter.ML;
using MassSpectrometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProteoformExplorer.Deconvoluter
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting");

            //var file = @"C:\Users\rmillikin\Desktop\MetaMorpheus Problems\TDHyPRMSdata_forRMandJP\BR1\raw\032621_Lysate_tr2.raw";
            //var file = @"C:\Users\rmillikin\Desktop\MetaMorpheus Problems\TDHyPRMSdata_forRMandJP\BR1\raw\032421_MALAT1Capture.raw";
            //var file = @"C:\Users\rmillikin\Desktop\MetaMorpheus Problems\TDHyPRMSdata_forRMandJP\BR1\raw\malat1_sliced.raw";
            //var file = @"C:\Users\rmillikin\Downloads\protein_mix_sid 50_iter5.raw";
            var file = @"C:\Users\rjmil\Desktop\MS data\05-26-17_B7A_yeast_td_fract7_rep2.raw";

            var data = IO.ThermoRawFileReader.ThermoRawFileReader.LoadAllStaticData(file);

            var engine = new DeconvolutionEngine(2000, 0.3, 6, 0.3, 5, 5, 2, 60, 2);

            var envs = engine.Deconvolute(data, file).ToList();

            //// run ML classifier
            //Console.WriteLine("Running ML");

            //Random r = new Random(0);
            //var harmonics = envs.Where(p => p.RetentionTime > 50 && p.RetentionTime < 100 && p.MonoisotopicMass > 6800 && p.MonoisotopicMass < 7100).OrderBy(p => r.Next()).ToList();
            //var falseHarmonics = envs.Where(p => p.RetentionTime > 180 && p.MonoisotopicMass > 20000).OrderBy(p => r.Next()).ToList();
            //var lowMassGarbage = envs.Where(p => p.Charge < 4).OrderBy(p => r.Next()).ToList();
            //var realEnvelopes = envs.Where(p => p.RetentionTime > 50 && p.RetentionTime < 100 && p.MonoisotopicMass > 11000 && p.MonoisotopicMass < 14500).OrderBy(p => r.Next()).ToList();

            //// write training/test data
            //List<string> trainingOutput = new List<string> { EnvelopeData.TabDelimitedHeader };
            //List<string> testingOutput = new List<string> { EnvelopeData.TabDelimitedHeader };

            //var training = harmonics.Take(500)
            //    .Concat(falseHarmonics.Take(500))
            //    .Concat(lowMassGarbage.Take(500))
            //    .Concat(realEnvelopes.Take(500))
            //    .ToList();

            //var testing = harmonics.Except(training).Take(500)
            //    .Concat(falseHarmonics.Except(training).Take(500))
            //    .Concat(lowMassGarbage.Except(training).Take(500))
            //    .Concat(realEnvelopes.Except(training).Take(500))
            //    .ToList();

            //foreach (var env in training.Concat(testing))
            //{
            //    var envelopeData = new EnvelopeData(env);

            //    string label = "";

            //    if (harmonics.Contains(env))
            //    {
            //        label = "harmonic";
            //    }
            //    else if (falseHarmonics.Contains(env))
            //    {
            //        label = "false harmonic";
            //    }
            //    else if (lowMassGarbage.Contains(env))
            //    {
            //        label = "low mass garbage";
            //    }
            //    else if (realEnvelopes.Contains(env))
            //    {
            //        label = "real envelope";
            //    }

            //    if (training.Contains(env))
            //    {
            //        trainingOutput.Add(envelopeData.ToOutputString(label));
            //    }
            //    else
            //    {
            //        testingOutput.Add(envelopeData.ToOutputString(label));
            //    }
            //}

            //var trainingPath = @"C:\Users\rmillikin\Desktop\MetaMorpheus Problems\TDHyPRMSdata_forRMandJP\BR1\raw\trainingData_" +
            //    Path.GetFileNameWithoutExtension(file) + ".csv";
            //var evaluationPath = @"C:\Users\rmillikin\Desktop\MetaMorpheus Problems\TDHyPRMSdata_forRMandJP\BR1\raw\testingData_" +
            //    Path.GetFileNameWithoutExtension(file) + ".csv";
            //var savedModelPath = @"C:\Users\rmillikin\Desktop\MetaMorpheus Problems\TDHyPRMSdata_forRMandJP\BR1\raw\trainedModel_" +
            //    Path.GetFileNameWithoutExtension(file) + ".zip";

            //File.WriteAllLines(trainingPath, trainingOutput);
            //File.WriteAllLines(evaluationPath, testingOutput);

            //// create, train, and test classifier
            //EnvelopeClassification classifier = new EnvelopeClassification();
            //classifier.TrainAndTestModel(trainingPath, evaluationPath);
            //classifier.SaveModel(savedModelPath);

            //// classify each envelope w/ the trained model
            //foreach (var envelope in envs)
            //{
            //    var envelopeData = new EnvelopeData(envelope);
            //    var prediction = classifier.Classify(envelopeData);
            //    envelope.MachineLearningClassification = prediction.Prediction;
            //    envelope.MachineLearningClassificationScores = prediction.Score;
            //}

            // write output
            Console.WriteLine("Writing output");

            var output = new List<string>() { DeconvolutedEnvelope.TabDelimitedHeader };

            foreach (var env in envs)
            {
                output.Add(env.ToOutputString());
            }

            File.WriteAllLines(@"C:\Users\rjmil\Desktop\MS data\decon_" +
                Path.GetFileNameWithoutExtension(file) + ".tsv", output);

            Console.WriteLine("Done");
        }
    }
}
