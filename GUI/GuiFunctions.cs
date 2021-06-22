using Chemistry;
using Deconvoluter;
using GUI.Modules;
using MassSpectrometry;
using MzLibUtil;
using mzPlot;
using ProteoformExplorer;
using ProteoformExplorerObjects;
using ScottPlot;
using ScottPlot.Drawing;
using ScottPlot.Statistics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace GUI
{
    public class GuiFunctions
    {
        // from: http://seaborn.pydata.org/tutorial/color_palettes.html (qualitative bright palette)
        public static string[] ColorPalette = new string[]
        {
            @"#00ca3d",  // green
            @"#e91405",  // red
            @"#9214e1",  // purple
            @"#9f4a00",  // brown
            @"#f44ac1",  // pink
            @"#fdc807",  // gold
            @"#17d4ff",  // teal
            @"#3729fe",  // blue
            @"#ff8000",  // orange
        };

        public static void PlotSpeciesInSpectrum(HashSet<AnnotatedSpecies> allSpeciesToPlot, int oneBasedScan, KeyValuePair<string, CachedSpectraFileData> data, WpfPlot spectrumPlot)
        {
            // set color palette
            spectrumPlot.Plot.Palette = new Palette(ColorPalette);
            spectrumPlot.Plot.Clear();
            spectrumPlot.Plot.Grid(false);
            spectrumPlot.Plot.YAxis.TickLabelNotation(multiplier: true);
            spectrumPlot.Plot.YAxis.Label("Intensity");
            spectrumPlot.Plot.XAxis.Label("m/z");

            // get the scan
            var scan = data.Value.GetOneBasedScan(oneBasedScan);

            spectrumPlot.Plot.Title(Path.GetFileName(data.Key) + "  Scan#" + scan.OneBasedScanNumber + "  RT: " + scan.RetentionTime + "  MS" + scan.MsnOrder + "\n" +
                " " + scan.ScanFilter, bold: false);

            // add non-annotated peaks
            List<Datum> spectrumData = new List<Datum>();

            for (int i = 0; i < scan.MassSpectrum.XArray.Length; i++)
            {
                spectrumData.Add(new Datum(scan.MassSpectrum.XArray[i], scan.MassSpectrum.YArray[i]));
            }

            foreach (var item in spectrumData)
            {
                spectrumPlot.Plot.AddLine(item.X, 0, item.X, item.Y.Value, Color.Gray, 1.0f);
            }

            if (allSpeciesToPlot == null)
            {
                return;
            }

            // add annotated peaks
            HashSet<double> claimedMzs = new HashSet<double>();
            foreach (var species in allSpeciesToPlot)
            {
                List<Datum> annotatedData = new List<Datum>();
                List<int> chargesToPlot = new List<int>();

                if (species.DeconvolutionFeature != null)
                {
                    double mass = Dashboard.DeconvolutionEngine.GetModeMassFromMonoisotopicMass(species.DeconvolutionFeature.MonoisotopicMass);
                    chargesToPlot.AddRange(species.DeconvolutionFeature.Charges);

                    foreach (var z in chargesToPlot)
                    {
                        int index = scan.MassSpectrum.GetClosestPeakIndex(mass.ToMz(z));
                        double expMz = scan.MassSpectrum.XArray[index];
                        double expIntensity = scan.MassSpectrum.YArray[index];

                        var envelope = Dashboard.DeconvolutionEngine.GetIsotopicEnvelope(scan.MassSpectrum, index, z, new List<Deconvoluter.DeconvolutedPeak>(),
                            claimedMzs, new List<(double, double)>());

                        if (envelope != null)
                        {
                            annotatedData.AddRange(envelope.Peaks.Select(p => new Datum(p.ExperimentalMz, p.ExperimentalIntensity)));
                        }
                        else
                        {
                            annotatedData.Add(new Datum(expMz, expIntensity));
                        }
                    }

                    foreach (var item in annotatedData)
                    {
                        claimedMzs.Add(item.X);
                    }
                }
                else if (species.Identification != null)
                {
                    double mass = Dashboard.DeconvolutionEngine.GetModeMassFromMonoisotopicMass(species.Identification.MonoisotopicMass);
                    int z = species.Identification.PrecursorChargeState;

                    int index = scan.MassSpectrum.GetClosestPeakIndex(mass.ToMz(z));
                    double expMz = scan.MassSpectrum.XArray[index];
                    double expIntensity = scan.MassSpectrum.YArray[index];

                    var envelope = Dashboard.DeconvolutionEngine.GetIsotopicEnvelope(scan.MassSpectrum, index, z, new List<Deconvoluter.DeconvolutedPeak>(),
                        claimedMzs, new List<(double, double)>());
                }

                var color = spectrumPlot.Plot.GetNextColor();

                foreach (var item in annotatedData)
                {
                    spectrumPlot.Plot.AddLine(item.X, 0, item.X, item.Y.Value, color, 2.0f);
                }
            }
        }

        public static void PlotXic(double mz, int z, Tolerance tolerance, double rt, double rtWindow, KeyValuePair<string, CachedSpectraFileData> data, WpfPlot xicPlot,
            bool clearOldPlot)
        {
            SetUpXicPlot(rt, rtWindow, data, xicPlot, clearOldPlot, out var scans);

            var xicData = GetXicData(scans, mz, z, tolerance);

            var xs = xicData.Select(p => p.X).ToArray();
            var ys = xicData.Select(p => p.Y.Value).ToArray();
            var color = xicPlot.Plot.GetNextColor();

            xicPlot.Plot.AddScatterLines(xs, ys, color);
        }

        public static void PlotSummedChargeStateXic(double modeMass, int z, double rt, double rtWindow, KeyValuePair<string, CachedSpectraFileData> data, WpfPlot xicPlot,
            bool clearOldPlot, double xOffset = 0, double yOffset = 0)
        {
            SetUpXicPlot(rt, rtWindow, data, xicPlot, clearOldPlot, out var scans);

            var xicData = GetSummedChargeXis(scans, modeMass, z);

            var xs = xicData.Select(p => p.X + xOffset).ToArray();
            var ys = xicData.Select(p => p.Y.Value + yOffset).ToArray();
            var color = xicPlot.Plot.GetNextColor();

            xicPlot.Plot.AddScatterLines(xs, ys, color, lineWidth: 2);

            if (clearOldPlot)
            {
                xicPlot.Plot.SetAxisLimitsY(Math.Min(0, ys.Min()), ys.Max());
                xicPlot.Plot.SetAxisLimitsX(xs.Min(), xs.Max());
            }
            else
            {
                var axisLimits = xicPlot.Plot.GetAxisLimits(xicPlot.Plot.XAxis.AxisIndex, xicPlot.Plot.YAxis.AxisIndex);
                double yMin = Math.Max(axisLimits.YMin, ys.Min());
                double yMax = Math.Max(axisLimits.YMax, ys.Max());
                double xMin = Math.Max(axisLimits.XMin, xs.Min());
                double xMax = Math.Max(axisLimits.XMax, xs.Max());

                xicPlot.Plot.SetAxisLimitsY(yMin, yMax);
                xicPlot.Plot.SetAxisLimitsX(xMin, xMax);
            }
        }

        public static void DrawPercentTicInfo(WpfPlot plot, out List<string> errors)
        {
            errors = new List<string>();

            try
            {
                plot.Plot.Title(@"MS1 TIC");

                // set color palette
                plot.Plot.Palette = new Palette(GuiFunctions.ColorPalette);

                List<(string file, double tic, double deconvolutedTic, double identifiedTic)> ticValues
                    = new List<(string file, double tic, double deconvolutedTic, double identifiedTic)>();

                foreach (var file in DataLoading.SpectraFiles)
                {
                    double tic = 0;
                    double deconvolutedTic = 0;
                    double identifiedTic = 0;

                    var ticChromatogram = file.Value.GetTicChromatogram();
                    if (ticChromatogram != null)
                    {
                        tic = ticChromatogram.Sum(p => p.Y.Value);
                    }

                    var deconvolutedTicChromatogram = file.Value.GetDeconvolutedTicChromatogram();
                    if (deconvolutedTicChromatogram != null)
                    {
                        deconvolutedTic = deconvolutedTicChromatogram.Sum(p => p.Y.Value);
                    }

                    var identifiedTicChromatogram = file.Value.GetIdentifiedTicChromatogram();
                    if (identifiedTicChromatogram != null)
                    {
                        identifiedTic = identifiedTicChromatogram.Sum(p => p.Y.Value);
                    }

                    ticValues.Add((Path.GetFileNameWithoutExtension(file.Key), tic, deconvolutedTic, identifiedTic));
                }

                double[] positions = Enumerable.Range(0, ticValues.Count).Select(p => (double)p).ToArray();
                string[] labels = ticValues.Select(p => p.file).ToArray();

                var ticPlot = new ScottPlot.Plottable.LollipopPlot(positions, ticValues.Select(p => p.tic).ToArray());
                ticPlot.Label = @"TIC";
                ticPlot.LollipopColor = Color.Black;
                ticPlot.LollipopRadius = 10;
                plot.Plot.Add(ticPlot);

                var deconTicPlot = new ScottPlot.Plottable.LollipopPlot(positions, ticValues.Select(p => p.deconvolutedTic).ToArray());
                deconTicPlot.Label = @"Deconvoluted TIC";
                deconTicPlot.LollipopColor = Color.Blue;
                deconTicPlot.LollipopRadius = 10;
                plot.Plot.Add(deconTicPlot);

                var identTicPlot = new ScottPlot.Plottable.LollipopPlot(positions, ticValues.Select(p => p.identifiedTic).ToArray());
                identTicPlot.Label = @"Identified TIC";
                identTicPlot.LollipopColor = Color.Purple;
                identTicPlot.LollipopRadius = 10;
                plot.Plot.Add(identTicPlot);

                plot.Plot.Legend(location: Alignment.UpperRight);

                plot.Plot.YAxis.Label("Intensity");
                plot.Plot.SetAxisLimitsY(ticValues.Max(p => p.tic) * -0.1, ticValues.Max(p => p.tic) * 1.4);
                plot.Plot.XTicks(positions, labels);
                plot.Plot.YAxis.TickLabelNotation(multiplier: true);
                plot.Plot.XAxis.TickLabelStyle(rotation: 30);
                plot.Plot.Grid(false);
                plot.Plot.YAxis.Ticks(major: true, minor: false);
            }
            catch (Exception e)
            {
                errors.Add(e.Message);
            }
        }

        public static void DrawNumEnvelopes(WpfPlot plot, out List<string> errors)
        {
            errors = new List<string>();

            try
            {
                plot.Plot.Title(@"MS1 Envelope Counts");
                // set color palette
                plot.Plot.Palette = new Palette(GuiFunctions.ColorPalette);

                var numFilteredEnvelopesPerFile = new List<(string file, int numFilteredEnvs)>();

                foreach (var file in DataLoading.SpectraFiles)
                {
                    int envs = file.Value.OneBasedScanToAnnotatedEnvelopes.Sum(p => p.Value.Count);
                    numFilteredEnvelopesPerFile.Add((Path.GetFileNameWithoutExtension(file.Key), envs));
                }

                double[] values = numFilteredEnvelopesPerFile.Select(p => (double)p.numFilteredEnvs).ToArray();
                double[] positions = Enumerable.Range(0, numFilteredEnvelopesPerFile.Count).Select(p => (double)p).ToArray();
                string[] labels = numFilteredEnvelopesPerFile.Select(p => p.file).ToArray();

                var envsCountPlot = new ScottPlot.Plottable.LollipopPlot(positions, values);
                envsCountPlot.Label = @"Deconvoluted Envelopes";
                envsCountPlot.LollipopColor = Color.Blue;
                envsCountPlot.LollipopRadius = 10;
                plot.Plot.Add(envsCountPlot);

                plot.Plot.Legend(location: Alignment.UpperRight);

                plot.Plot.YAxis.Label("Count");
                plot.Plot.SetAxisLimitsY(0, numFilteredEnvelopesPerFile.Max(p => p.numFilteredEnvs) * 1.2);
                plot.Plot.XTicks(positions, labels);
                plot.Plot.XAxis.TickLabelStyle(rotation: 30);
                plot.Plot.Grid(false);
                plot.Plot.YAxis.Ticks(major: true, minor: false);
            }
            catch (Exception e)
            {
                errors.Add(e.Message);
            }
        }

        public static void DrawMassDistributions(WpfPlot plot, out List<string> errors)
        {
            errors = new List<string>();

            try
            {
                plot.Plot.Title(@"MS1 Envelope Mass Histograms");
                plot.Plot.Palette = new Palette(GuiFunctions.ColorPalette);
                var color = Color.Blue;

                int fileNum = 0;
                double maxMass = 0;
                foreach (var file in DataLoading.SpectraFiles)
                {
                    //TODO: decon feature could be null
                    var envelopeMasses = file.Value.OneBasedScanToAnnotatedEnvelopes.SelectMany(p => p.Value.Select(v => v.PeakMzs.First().ToMass(v.Charge))).ToArray();
                    maxMass = Math.Max(maxMass, envelopeMasses.Max());

                    var hist = new ScottPlot.Statistics.Histogram(envelopeMasses, binSize: 500);

                    var bar = plot.Plot.AddBar(values: hist.countsFrac, positions: hist.bins);
                    bar.BarWidth = hist.binSize;
                    bar.FillColor = Color.FromArgb(180, color);
                    bar.BorderLineWidth = 0;
                    bar.Orientation = ScottPlot.Orientation.Horizontal;
                    bar.ValueOffsets = hist.countsFrac.Select(p => (double)fileNum).ToArray();
                    bar.Label = Path.GetFileNameWithoutExtension(file.Key);

                    fileNum++;
                }

                plot.Plot.YAxis.Label("Daltons");
                plot.Plot.SetAxisLimitsY(0, maxMass * 1.2);

                double[] xPositions = Enumerable.Range(0, DataLoading.SpectraFiles.Count).Select(p => (double)p).ToArray();
                string[] xLabels = DataLoading.SpectraFiles.Select(p => Path.GetFileNameWithoutExtension(p.Key)).ToArray();
                plot.Plot.XTicks(xPositions, xLabels);
                plot.Plot.XAxis.TickLabelStyle(rotation: 30);
                plot.Plot.Grid(false);
                plot.Plot.YAxis.Ticks(major: true, minor: false);
            }
            catch (Exception e)
            {
                errors.Add(e.Message);
            }
        }

        private static List<Datum> GetXicData(List<MsDataScan> scans, double mz, int z, Tolerance tolerance)
        {
            List<Datum> xicData = new List<Datum>();
            foreach (var scan in scans)
            {
                int ind = scan.MassSpectrum.GetClosestPeakIndex(mz);
                double expMz = scan.MassSpectrum.XArray[ind];
                double expIntensity = scan.MassSpectrum.YArray[ind];

                if (tolerance.Within(expMz.ToMass(z), mz.ToMass(z)))
                {
                    xicData.Add(new Datum(scan.RetentionTime, expIntensity));
                }
                else
                {
                    xicData.Add(new Datum(scan.RetentionTime, 0));
                }
            }

            return xicData;
        }

        private static List<Datum> GetSummedChargeXis(List<MsDataScan> scans, double modeMass, int z)
        {
            List<Datum> xicData = new List<Datum>();
            List<DeconvolutedPeak> peaks = new List<DeconvolutedPeak>();
            HashSet<double> temp = new HashSet<double>();
            List<(double, double)> temp2 = new List<(double, double)>();

            foreach (var scan in scans)
            {
                int ind = scan.MassSpectrum.GetClosestPeakIndex(modeMass.ToMz(z));
                double expMz = scan.MassSpectrum.XArray[ind];
                double expIntensity = scan.MassSpectrum.YArray[ind];

                var env = Dashboard.DeconvolutionEngine.GetIsotopicEnvelope(scan.MassSpectrum, ind, z, peaks, temp, temp2);

                if (env != null)
                {
                    xicData.Add(new Datum(scan.RetentionTime, env.Peaks.Sum(p => p.ExperimentalIntensity)));
                }
                else
                {
                    xicData.Add(new Datum(scan.RetentionTime, 0));
                }
            }

            return xicData;
        }

        private static void SetUpXicPlot(double rt, double rtWindow, KeyValuePair<string, CachedSpectraFileData> data,
            WpfPlot xicPlot, bool clearOldPlot, out List<MsDataScan> scans)
        {
            if (clearOldPlot)
            {
                xicPlot.Plot.Clear();
            }

            xicPlot.Plot.Palette = new Palette(ColorPalette);
            xicPlot.Plot.Grid(false);
            xicPlot.Plot.YAxis.TickLabelNotation(multiplier: true);
            xicPlot.Plot.YAxis.Label("Intensity");
            xicPlot.Plot.XAxis.Label("Retention Time");

            double rtWindowHalfWidth = rtWindow / 2;

            var xic = data.Value.GetTicChromatogram();
            var firstScan = xic.First(p => p.X > rt - rtWindowHalfWidth);

            scans = new List<MsDataScan>();
            for (int i = xic.IndexOf(firstScan); i < xic.Count; i++)
            {
                int scanNum = (int)Math.Round(xic[i].Z.Value);
                var theScan = data.Value.GetOneBasedScan(scanNum);

                if (theScan.MsnOrder == 1)
                {
                    scans.Add(theScan);
                }

                if (theScan.RetentionTime >= rt + rtWindowHalfWidth)
                {
                    break;
                }
            }
        }
    }
}
