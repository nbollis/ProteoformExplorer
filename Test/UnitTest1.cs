using Chemistry;
using Deconvoluter;
using IO.ThermoRawFileReader;
using MassSpectrometry;
using NUnit.Framework;
using ProteoformExplorer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Test
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestReadingMetaMorpheus()
        {
            string filePath = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestData\MetaMorpheus.tsv");
            var species = InputReaderParser.ReadSpeciesFromFile(filePath, out var errors);

            Assert.That(!errors.Any());
            Assert.That(species.Count == 72);
        }

        [Test]
        public void TestReadingTdPortal()
        {
            string filePath = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestData\td_portal.tsv");
            var species = InputReaderParser.ReadSpeciesFromFile(filePath, out var errors);

            Assert.That(!errors.Any());
            Assert.That(species.Count == 12);
        }

        [Test]
        public void TestReadingFlashDecon()
        {
            string filePath = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestData\FlashDeconTest.tsv");
            var species = InputReaderParser.ReadSpeciesFromFile(filePath, out var errors);

            Assert.That(!errors.Any());
            Assert.That(species.Count == 10);
        }

        [Test]
        public void TestReadingThermoDecon()
        {
            string filePath = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestData\3-19-18_MCF7_IM_br1_f1_2_results+_calibrated.txt");
            var species = InputReaderParser.ReadSpeciesFromFile(filePath, out var errors);

            Assert.That(!errors.Any());
            Assert.That(species.Count == 20);
            Assert.That(species.First().DeconvolutionFeature.SpectraFileNameWithoutExtension == "3-19-18_MCF7_IM_br1_f1_2_results+_calibrated");
        }

        [Test]
        public void TestDynamicConnectionRtBinarySearch()
        {
            string rawFile = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestData\mcf7_sliced\mcf7_sliced_td.raw");
            var connection = new KeyValuePair<string, DynamicDataConnection>(rawFile, new ThermoDynamicData(rawFile));

            var lastScanNum = PfmXplorerUtil.GetLastOneBasedScanNumber(connection);
            Assert.That(lastScanNum == 52);

            var scan = PfmXplorerUtil.GetClosestScanToRtFromDynamicConnection(connection, 57.0);
            Assert.That(scan.OneBasedScanNumber == 1);

            scan = PfmXplorerUtil.GetClosestScanToRtFromDynamicConnection(connection, 58.21);
            Assert.That(scan.OneBasedScanNumber == 30);

            scan = PfmXplorerUtil.GetClosestScanToRtFromDynamicConnection(connection, 59.0);
            Assert.That(scan.OneBasedScanNumber == 52);

            connection.Value.CloseDynamicConnection();
        }

        [Test]
        public static void TestDeconvolution()
        {
            UsefulProteomicsDatabases.Loaders.LoadElements();

            // >sp|P49703|ARL4D_HUMAN ADP-ribosylation factor-like protein 4D OS=Homo sapiens OX=9606 GN=ARL4D PE=1 SV=2
            string sequence = "MGNHLTEMAPTASSFLPHFQALHVVVIGLDSAGKTSLLYRLKFKEFVQSVPTKGFNTEKIRVPLGGSRGITFQ" +
                              "VWDVGGQEKLRPLWRSYTRRTDGLVFVVDAAEAERLEEAKVELHRISRASDNQGVPVLVLANKQDQPGALSAA" +
                              "EVEKRLAVRELAAATLTHVQGCSAVDGLGLQQGLERLYEMILKRKKAARGGKKRR";
            int charge = 15;
            double intensityMultiplier = 1e6;

            Proteomics.AminoAcidPolymer.Peptide baseSequence = new Proteomics.AminoAcidPolymer.Peptide(sequence);
            var formula = baseSequence.GetChemicalFormula();
            var isotopicDistribution = IsotopicDistribution.GetDistribution(formula, 0.125, 1e-8);

            double[] masses = isotopicDistribution.Masses.ToArray();
            double[] abundances = isotopicDistribution.Intensities.ToArray();
            double max = abundances.Max();

            List<(double, double)> peaks = new List<(double, double)>();

            for (int i = 0; i < masses.Length; i++)
            {
                abundances[i] /= max;

                if (abundances[i] >= 0.05)
                {
                    peaks.Add((masses[i].ToMz(charge), abundances[i] * intensityMultiplier));
                }
            }

            Random r = new Random(1);
            for (int i = 0; i < 1000; i++)
            {
                double mz = r.NextDouble() * 1200 + 400;
                double intensity = r.NextDouble() * 30000 + 30000;
                peaks.Add((mz, intensity));
            }

            peaks = peaks.OrderBy(p => p.Item1).ToList();

            var spectrum = new MzSpectrum(peaks.Select(p => p.Item1).ToArray(), peaks.Select(p => p.Item2).ToArray(), true);

            var engine = new DeconvolutionEngine(0, 0.4, 3, 0.4, 1.5, 5, 1, 60, 2);
            var envs = engine.Deconvolute(spectrum, spectrum.Range).ToList();

            List<string> output = new List<string> { DeconvolutedEnvelope.TabDelimitedHeader };

            foreach (var env in envs)
            {
                env.SpectraFileName = "temp";
                output.Add(env.ToOutputString());
            }

            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestPfmExplorerDeconOutput.tsv");
            File.WriteAllLines(path, output);

            var species = InputReaderParser.ReadSpeciesFromFile(path, out var errors);

            Assert.That(!errors.Any());
            Assert.That(species.Count > 0);
            Assert.That(species.All(p => p.DeconvolutionFeature != null));
            Assert.That(species.All(p => p.DeconvolutionFeature.AnnotatedEnvelopes != null && p.DeconvolutionFeature.AnnotatedEnvelopes.Count > 0));
            Assert.That(species.All(p => p.DeconvolutionFeature.AnnotatedEnvelopes.All(v => v.PeakMzs.Count > 0)));
            
            //Assert.That(species.Count == 1);
        }

        [Test]
        public static void TestDeconvolutionRealFile()
        {
            string filePath = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestData\mcf7_sliced\mcf7_sliced_td.raw");
            var data = IO.ThermoRawFileReader.ThermoRawFileReader.LoadAllStaticData(filePath);
            var deconEngine = new DeconvolutionEngine(2000, 0.4, 3, 0.4, 3, 5, 2, 60, 3);
            var envelopes = deconEngine.Deconvolute(data, filePath, 1).ToList();

            List<string> output = new List<string>() { DeconvolutedEnvelope.TabDelimitedHeader };

            foreach (var env in envelopes)
            {
                output.Add(env.ToOutputString());
            }

            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestPfmExplorerDeconOutput_mcf7.tsv");
            File.WriteAllLines(path, output);

            var species = InputReaderParser.ReadSpeciesFromFile(path, out var errors);

            Assert.That(!errors.Any());
            Assert.That(species.Count > 0);
            Assert.That(species.All(p => p.DeconvolutionFeature != null));
            Assert.That(species.All(p => p.DeconvolutionFeature.AnnotatedEnvelopes != null && p.DeconvolutionFeature.AnnotatedEnvelopes.Count > 0));
            Assert.That(species.All(p => p.DeconvolutionFeature.AnnotatedEnvelopes.All(v => v.PeakMzs.Count > 0)));
            Assert.That(species.All(p => p.DeconvolutionFeature.SpectraFileNameWithoutExtension == Path.GetFileNameWithoutExtension(filePath)));
        }

        //[Test]
        //public void TestMetaMorpheusOutputWithRaw()
        //{
        //    string filePath = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestData\mcf7_sliced\AllPSMs.psmtsv");
        //    string rawFile = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestData\mcf7_sliced\mcf7_sliced_td.raw");

        //    var species = InputReaderParser.ReadSpeciesFromFile(filePath, out var errors);
        //}
    }
}