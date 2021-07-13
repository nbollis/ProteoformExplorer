using Chemistry;
using ProteoformExplorer.Deconvoluter;
using IO.ThermoRawFileReader;
using MassSpectrometry;
using NUnit.Framework;
using ProteoformExplorer.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UsefulProteomicsDatabases;

namespace Test
{
    public class Tests
    {
        [OneTimeSetUp]
        public void Setup()
        {
            Loaders.LoadElements();
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

            species = species.Where(p => (int)p.DeconvolutionFeature.MonoisotopicMass == 15229).ToList();
            string rawFile = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestData\05-26-17_B7A_yeast_td_fract7_rep1.raw");
            var connection = new KeyValuePair<string, DynamicDataConnection>(rawFile, new ThermoDynamicData(rawFile));
            var cachedData = new KeyValuePair<string, CachedSpectraFileData>(rawFile, new CachedSpectraFileData(connection));
            cachedData.Value.CreateAnnotatedDeconvolutionFeatures(species);

            Assert.That(species.First().DeconvolutionFeature.AnnotatedEnvelopes.Count >= 1);
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
            var cachedData = new KeyValuePair<string, CachedSpectraFileData>(rawFile, new CachedSpectraFileData(connection));

            var lastScanNum = PfmXplorerUtil.GetLastOneBasedScanNumber(cachedData);
            Assert.That(lastScanNum == 52);

            var scan = PfmXplorerUtil.GetClosestScanToRtFromDynamicConnection(cachedData, 57.0);
            Assert.That(scan.OneBasedScanNumber == 1);

            scan = PfmXplorerUtil.GetClosestScanToRtFromDynamicConnection(cachedData, 58.21);
            Assert.That(scan.OneBasedScanNumber == 30);

            scan = PfmXplorerUtil.GetClosestScanToRtFromDynamicConnection(cachedData, 59.0);
            Assert.That(scan.OneBasedScanNumber == 52);

            connection.Value.CloseDynamicConnection();
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

            // populate the decon feature with data from the spectra file
            var file = new CachedSpectraFileData(new KeyValuePair<string, DynamicDataConnection>(filePath, new ThermoDynamicData(filePath)));
            file.CreateAnnotatedDeconvolutionFeatures(species);

            Assert.That(!errors.Any());
            Assert.That(species.Count > 0);
            Assert.That(species.All(p => p.DeconvolutionFeature != null));
            var debug = species.Where(p => p.DeconvolutionFeature.AnnotatedEnvelopes == null || p.DeconvolutionFeature.AnnotatedEnvelopes.Count == 0).ToList();

            Assert.That(species.All(p => p.DeconvolutionFeature.AnnotatedEnvelopes != null && p.DeconvolutionFeature.AnnotatedEnvelopes.Count > 0));
            Assert.That(species.All(p => p.DeconvolutionFeature.AnnotatedEnvelopes.All(v => v.PeakMzs.Count > 0)));
            Assert.That(species.All(p => p.DeconvolutionFeature.SpectraFileNameWithoutExtension == PfmXplorerUtil.GetFileNameWithoutExtension(filePath)));
        }

        //[Test]
        //[TestCase(@"032421_MALAT1Capture_11903_30632", 1136.1993, 30632.139, 27)] // charge too high
        //[TestCase(@"032421_MALAT1Capture_3033_5648", 628.9741, 5648.693, 9)] // very noisy/difficult

        //[TestCase(@"032421_MALAT1Capture_3090_13765", 689.6843, 13765.519, 20)] // easy case
        //[TestCase(@"032421_MALAT1Capture_5081_7011", 638.7745, 7011.428, 11)] // charge too low
        //[TestCase(@"032421_MALAT1Capture_5790_7045", 784.3348, 7045.937, 9)] // charge too low

        //public static void TestDeconvolutionDifferentCases(string dataFilePath, double mz, double monoMass, int z)
        //{
        //    string filePath = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestData\DeconCases\" + dataFilePath + ".mzML");
        //    var data = IO.MzML.Mzml.LoadAllStaticData(filePath);
        //    var deconEngine = new DeconvolutionEngine(2000, 0.3, 6, 0.4, 3, 5, 2, 60, 2);

        //    var scan = data.GetAllScansList().First();

        //    Tolerance t = new AbsoluteTolerance(0.001);

        //    var candidates = deconEngine.GetEnvelopeCandidates(scan.MassSpectrum, scan.ScanWindowRange);
        //    var candidatesWithMz = candidates.Where(p => p.Peaks.Any(v => t.Within(v.ExperimentalMz, mz))).OrderByDescending(p => p.Score).ToList();

        //    var parsimonyEnvelopes = deconEngine.RunEnvelopeParsimony(candidates, scan.MassSpectrum);
        //    var parsimonyEnvsWithMz = parsimonyEnvelopes.Where(p => p.Peaks.Any(v => t.Within(v.ExperimentalMz, mz))).ToList();
        //}
    }
}