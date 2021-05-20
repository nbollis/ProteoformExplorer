using IO.ThermoRawFileReader;
using MassSpectrometry;
using NUnit.Framework;
using ProteoformExplorer;
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

        //[Test]
        //public void TestMetaMorpheusOutputWithRaw()
        //{
        //    string filePath = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestData\mcf7_sliced\AllPSMs.psmtsv");
        //    string rawFile = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestData\mcf7_sliced\mcf7_sliced_td.raw");

        //    var species = InputReaderParser.ReadSpeciesFromFile(filePath, out var errors);
        //}
    }
}