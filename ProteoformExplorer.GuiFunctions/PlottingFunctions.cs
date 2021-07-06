using Chemistry;
using ProteoformExplorer.Deconvoluter;
using MassSpectrometry;
using MzLibUtil;
using ProteoformExplorer.Core;
using ScottPlot;
using ScottPlot.Drawing;
using ScottPlot.Plottable;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;

namespace ProteoformExplorer.GuiFunctions
{
    public class PlottingFunctions
    {
        public static void StylePlot(Plot plot)
        {
            plot.Palette = new Palette(GuiSettings.ChartColorPalette);
            plot.Grid(GuiSettings.ShowChartGrid);
            plot.XAxis.Ticks(major: true, minor: false);
            plot.YAxis.Ticks(major: true, minor: false);
            plot.Legend(location: GuiSettings.LegendLocation);
            plot.XAxis.TickMarkColor(Color.Transparent);
            plot.YAxis.TickMarkColor(Color.Transparent);

            plot.XAxis.Line(width: (float)GuiSettings.DpiScalingY);
            plot.XAxis.Label(size: (float)(GuiSettings.ChartLabelFontSize * GuiSettings.DpiScalingY)
                //    , bold: true
                );
            plot.XAxis.TickLabelStyle(fontSize: (float)(GuiSettings.ChartTickFontSize * GuiSettings.DpiScalingY)
                //    , fontBold: true
                );

            plot.YAxis.Line(width: (float)GuiSettings.DpiScalingX);
            plot.YAxis.Label(size: (float)(GuiSettings.ChartLabelFontSize * GuiSettings.DpiScalingX)
                //    , bold: true
                );
            plot.YAxis.TickLabelStyle(fontSize: (float)(GuiSettings.ChartTickFontSize * GuiSettings.DpiScalingX)
                //    , fontBold: true
                );

            var legend = plot.Legend(location: GuiSettings.LegendLocation);
            legend.FontSize = (float)(GuiSettings.ChartLegendFontSize * GuiSettings.DpiScalingX);

            // title
            plot.XAxis2.Label(size: (float)(GuiSettings.ChartHeaderFontSize * GuiSettings.DpiScalingX));

            foreach (var item in plot.GetPlottables())
            {
                if (item is ScatterPlot scatter)
                {
                    //TODO: this will affect all lines... spectra and xic..
                    scatter.LineWidth = GuiSettings.ChartLineWidth * GuiSettings.DpiScalingY;
                }
                else if (item is LollipopPlot lollipopPlot)
                {
                    //TODO
                    lollipopPlot.ErrorLineWidth = (float)(GuiSettings.ChartLineWidth * GuiSettings.DpiScalingX);
                    lollipopPlot.BarWidth = (float)(GuiSettings.ChartLineWidth * GuiSettings.DpiScalingX);
                }
            }
        }

        public static void StyleDashboardPlot(Plot plot)
        {
            StylePlot(plot);

            plot.XAxis.TickLabelStyle(rotation: (float)GuiSettings.XLabelRotation);
            plot.Frame(top: false, right: false);
            plot.YAxis.Line(visible: false);
        }

        public static void StyleIonChromatogramPlot(Plot plot, VLine rtIndicator)
        {
            if (rtIndicator != null)
            {
                plot.Add(rtIndicator);
            }

            StylePlot(plot);

            plot.YAxis.TickLabelNotation(multiplier: true);
            plot.YAxis.Label("Intensity");
            plot.XAxis.Label("Retention Time");
            plot.Layout(padding: 10);
        }

        public static void StyleSpectrumPlot(Plot plot, MsDataScan scan, string dataFileName)
        {
            StylePlot(plot);

            try
            {
                plot.Title(dataFileName + "  Scan#" + scan.OneBasedScanNumber + "  RT: " + scan.RetentionTime + "  MS" + scan.MsnOrder + "\n" +
                    " " + scan.ScanFilter, bold: false);
            }
            catch (Exception e)
            {
                plot.Title(dataFileName + "  Scan#" + scan.OneBasedScanNumber + "  RT: " + scan.RetentionTime + "  MS" + scan.MsnOrder, bold: false);
            }

            plot.YAxis.TickLabelNotation(multiplier: true);
            plot.YAxis.Label("Intensity");
            plot.XAxis.Label("m/z");
            plot.SetViewLimits(scan.ScanWindowRange.Minimum, scan.ScanWindowRange.Maximum, 0, scan.MassSpectrum.YArray.Max() * 2.0);
        }

        public static void PlotSpeciesInSpectrum(HashSet<AnnotatedSpecies> allSpeciesToPlot, int oneBasedScan, KeyValuePair<string, CachedSpectraFileData> data,
            Plot spectrumPlot, out MsDataScan scan, out List<string> errors, int? charge = null)
        {
            scan = null;
            errors = new List<string>();

            try
            {
                spectrumPlot.Clear();

                if (data.Value == null)
                {
                    return;
                }

                // get the scan
                scan = data.Value.GetOneBasedScan(oneBasedScan);

                if (scan == null)
                {
                    return;
                }

                // add non-annotated peaks
                List<Datum> spectrumData = new List<Datum>();

                for (int i = 0; i < scan.MassSpectrum.XArray.Length; i++)
                {
                    spectrumData.Add(new Datum(scan.MassSpectrum.XArray[i], scan.MassSpectrum.YArray[i]));
                }

                foreach (var item in spectrumData)
                {
                    spectrumPlot.AddLine(item.X, 0, item.X, item.Y.Value, GuiSettings.UnannotatedSpectrumColor, (float)GuiSettings.ChartLineWidth);
                }

                if (allSpeciesToPlot != null && scan.MsnOrder == 1)
                {
                    // add annotated peaks
                    HashSet<double> claimedMzs = new HashSet<double>();
                    foreach (var species in allSpeciesToPlot)
                    {
                        List<Datum> annotatedData = new List<Datum>();
                        List<int> chargesToPlot = new List<int>();

                        if (species.DeconvolutionFeature != null)
                        {
                            double mass = PfmXplorerUtil.DeconvolutionEngine.GetModeMassFromMonoisotopicMass(species.DeconvolutionFeature.MonoisotopicMass);

                            if (charge != null)
                            {
                                chargesToPlot.Add(charge.Value);
                            }
                            else
                            {
                                chargesToPlot.AddRange(species.DeconvolutionFeature.Charges);
                            }

                            foreach (var z in chargesToPlot)
                            {
                                int index = scan.MassSpectrum.GetClosestPeakIndex(mass.ToMz(z));
                                double expMz = scan.MassSpectrum.XArray[index];
                                double expIntensity = scan.MassSpectrum.YArray[index];

                                var envelope = PfmXplorerUtil.DeconvolutionEngine.GetIsotopicEnvelope(scan.MassSpectrum, index, z, new List<Deconvoluter.DeconvolutedPeak>(),
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
                            double mass = PfmXplorerUtil.DeconvolutionEngine.GetModeMassFromMonoisotopicMass(species.Identification.MonoisotopicMass);
                            int z = species.Identification.PrecursorChargeState;

                            int index = scan.MassSpectrum.GetClosestPeakIndex(mass.ToMz(z));
                            double expMz = scan.MassSpectrum.XArray[index];
                            double expIntensity = scan.MassSpectrum.YArray[index];

                            var envelope = PfmXplorerUtil.DeconvolutionEngine.GetIsotopicEnvelope(scan.MassSpectrum, index, z, new List<Deconvoluter.DeconvolutedPeak>(),
                                claimedMzs, new List<(double, double)>());
                        }

                        var color = spectrumPlot.GetNextColor();

                        foreach (var item in annotatedData)
                        {
                            spectrumPlot.AddLine(item.X, 0, item.X, item.Y.Value, color, (float)GuiSettings.AnnotatedEnvelopeLineWidth);
                        }
                    }
                }

                StyleSpectrumPlot(spectrumPlot, scan, data.Key);
                spectrumPlot.SetAxisLimits(scan.ScanWindowRange.Minimum, scan.ScanWindowRange.Maximum, 0, scan.MassSpectrum.YArray.Max() * 1.2);
            }
            catch (Exception e)
            {
                errors.Add("An error occurred while plotting the spectrum: " + e.Message);
            }
        }

        public static void PlotXic(double mz, int z, Tolerance tolerance, double rt, double rtWindow, KeyValuePair<string, CachedSpectraFileData> data, Plot xicPlot,
            bool clearOldPlot, VLine rtIndicator, out List<string> errors, double xOffset = 0, double yOffset = 0, string label = null)
        {
            errors = new List<string>();

            try
            {
                if (clearOldPlot)
                {
                    xicPlot.Clear();
                }

                var scans = GetScansInRtWindow(rt, rtWindow, data);

                var xicData = GetXicData(scans, mz, z, tolerance);

                var xs = xicData.Select(p => p.X).ToArray();
                var ys = xicData.Select(p => p.Y.Value).ToArray();
                var color = xicPlot.GetNextColor();

                xicPlot.AddScatterLines(xs, ys, color, label: label);
                xicPlot.Legend(label != null, location: GuiSettings.LegendLocation);

                bool isWaterfall = xOffset != 0 || yOffset != 0;

                if ((isWaterfall && GuiSettings.FillWaterfall) || (!isWaterfall && GuiSettings.FillSideView))
                {
                    var colorWithTransparency = Color.FromArgb(GuiSettings.FillAlpha, color.R, color.G, color.B);
                    xicPlot.AddFill(xs, ys, baseline: yOffset, color: colorWithTransparency);
                }

                if (clearOldPlot)
                {
                    xicPlot.SetAxisLimitsY(Math.Min(0, ys.Min()), ys.Max());
                    xicPlot.SetAxisLimitsX(xs.Min(), xs.Max());
                }
                else
                {
                    var axisLimits = xicPlot.GetAxisLimits(xicPlot.XAxis.AxisIndex, xicPlot.YAxis.AxisIndex);
                    double yMin = Math.Max(axisLimits.YMin, ys.Min());
                    double yMax = Math.Max(axisLimits.YMax, ys.Max());
                    double xMin = Math.Max(axisLimits.XMin, xs.Min());
                    double xMax = Math.Max(axisLimits.XMax, xs.Max());

                    xicPlot.SetAxisLimitsY(yMin, yMax);
                    xicPlot.SetAxisLimitsX(xMin, xMax);

                    //xicPlot.Plot.SetViewLimits(xMin, xMax, yMin, yMax * 2);
                }

                StyleIonChromatogramPlot(xicPlot, rtIndicator);
            }
            catch (Exception e)
            {
                errors.Add(e.Message);
            }
        }

        public static void PlotSummedChargeStateXic(double modeMass, int z, double rt, double rtWindow, KeyValuePair<string, CachedSpectraFileData> data, Plot xicPlot,
            bool clearOldPlot, VLine rtIndicator, out List<string> errors, double xOffset = 0, double yOffset = 0, string label = null)
        {
            errors = new List<string>();

            try
            {

                if (clearOldPlot)
                {
                    xicPlot.Clear();
                }

                var scans = GetScansInRtWindow(rt, rtWindow, data);

                var xicData = GetSummedChargeXics(scans, modeMass, z);

                var xs = xicData.Select(p => p.X + xOffset).ToArray();
                var ys = xicData.Select(p => p.Y.Value + yOffset).ToArray();
                var color = xicPlot.GetNextColor();

                xicPlot.AddScatterLines(xs, ys, color, lineWidth: (float)GuiSettings.ChartLineWidth, label: label);
                xicPlot.Legend(label != null, location: GuiSettings.LegendLocation);

                bool isWaterfall = xOffset != 0 || yOffset != 0;

                if ((isWaterfall && GuiSettings.FillWaterfall) || (!isWaterfall && GuiSettings.FillSideView))
                {
                    var colorWithTransparency = Color.FromArgb(GuiSettings.FillAlpha, color.R, color.G, color.B);
                    xicPlot.AddFill(xs, ys, baseline: yOffset, color: colorWithTransparency);
                }

                if (clearOldPlot)
                {
                    xicPlot.SetAxisLimitsY(Math.Min(0, ys.Min()), ys.Max());
                    xicPlot.SetAxisLimitsX(xs.Min(), xs.Max());
                }
                else
                {
                    var axisLimits = xicPlot.GetAxisLimits(xicPlot.XAxis.AxisIndex, xicPlot.YAxis.AxisIndex);
                    double yMin = Math.Max(axisLimits.YMin, ys.Min());
                    double yMax = Math.Max(axisLimits.YMax, ys.Max());
                    double xMin = Math.Max(axisLimits.XMin, xs.Min());
                    double xMax = Math.Max(axisLimits.XMax, xs.Max());

                    xicPlot.SetAxisLimitsY(yMin, yMax);
                    xicPlot.SetAxisLimitsX(xMin, xMax);

                    //xicPlot.Plot.SetViewLimits(xMin, xMax, yMin, yMax * 2);
                }

                StyleIonChromatogramPlot(xicPlot, rtIndicator);
            }
            catch (Exception e)
            {
                errors.Add(e.Message);
            }
        }

        public static void PlotTotalIonChromatograms(Plot plot, VLine rtIndicator, out List<string> errors)
        {
            errors = new List<string>();
            plot.Clear();

            try
            {
                if (DataManagement.CurrentlySelectedFile.Value == null)
                {
                    return;
                }

                // display TIC chromatogram
                var ticChromatogram = DataManagement.CurrentlySelectedFile.Value.GetTicChromatogram();

                plot.AddScatterLines(
                    ticChromatogram.Select(p => p.X).ToArray(),
                    ticChromatogram.Select(p => p.Y.Value).ToArray(),
                    GuiSettings.TicColor, (float)GuiSettings.ChartLineWidth, label: "TIC");

                // display identified TIC chromatogram
                if (DataManagement.AllLoadedAnnotatedSpecies.Any())
                {
                    var identifiedTicChromatogram = DataManagement.CurrentlySelectedFile.Value.GetIdentifiedTicChromatogram();

                    if (identifiedTicChromatogram.Any())
                    {
                        plot.AddScatterLines(
                            identifiedTicChromatogram.Select(p => p.X).ToArray(),
                            identifiedTicChromatogram.Select(p => p.Y.Value).ToArray(),
                            GuiSettings.IdentifiedColor, (float)GuiSettings.ChartLineWidth, label: "Identified TIC");
                    }

                    var deconvolutedTicChromatogram = DataManagement.CurrentlySelectedFile.Value.GetDeconvolutedTicChromatogram();

                    if (deconvolutedTicChromatogram.Any())
                    {
                        plot.AddScatterLines(
                            deconvolutedTicChromatogram.Select(p => p.X).ToArray(),
                            deconvolutedTicChromatogram.Select(p => p.Y.Value).ToArray(),
                            GuiSettings.DeconvolutedColor, (float)GuiSettings.ChartLineWidth, label: "Deconvoluted TIC");
                    }
                }

                plot.SetViewLimits(xMin: ticChromatogram.Min(p => p.X), xMax: ticChromatogram.Max(p => p.X), yMin: 0, yMax: ticChromatogram.Max(p => p.Y.Value) * 2.0);

                StyleIonChromatogramPlot(plot, rtIndicator);
            }
            catch (Exception e)
            {
                errors.Add(e.Message);
            }
        }

        public static void DrawPercentTicPerFileInfoDashboardPlot(Plot plot, out List<string> errors)
        {
            plot.Clear();
            errors = new List<string>();

            try
            {
                plot.Title(@"MS1 TIC");

                var ticValues = new List<(string file, double tic, double deconvolutedTic, double identifiedTic)>();

                foreach (var file in DataManagement.SpectraFiles)
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
                ticPlot.LollipopColor = GuiSettings.TicColor;
                ticPlot.LollipopRadius = 10;
                plot.Add(ticPlot);

                var deconTicPlot = new ScottPlot.Plottable.LollipopPlot(positions, ticValues.Select(p => p.deconvolutedTic).ToArray());
                deconTicPlot.Label = @"Deconvoluted TIC";
                deconTicPlot.LollipopColor = GuiSettings.DeconvolutedColor;
                deconTicPlot.LollipopRadius = 10;
                plot.Add(deconTicPlot);

                var identTicPlot = new ScottPlot.Plottable.LollipopPlot(positions, ticValues.Select(p => p.identifiedTic).ToArray());
                identTicPlot.Label = @"Identified TIC";
                identTicPlot.LollipopColor = GuiSettings.IdentifiedColor;
                identTicPlot.LollipopRadius = 10;
                plot.Add(identTicPlot);

                plot.YAxis.Label("Intensity");
                plot.SetAxisLimitsY(ticValues.Max(p => p.tic) * -0.03, ticValues.Max(p => p.tic) * 1.4);
                plot.XTicks(positions, labels);
                plot.YAxis.TickLabelNotation(multiplier: true);

                StyleDashboardPlot(plot);
            }
            catch (Exception e)
            {
                errors.Add(e.Message);
            }
        }

        public static void DrawNumEnvelopesDashboardPlot(Plot plot, out List<string> errors)
        {
            plot.Clear();
            errors = new List<string>();

            try
            {
                plot.Title(@"MS1 Envelope Counts");

                var numFilteredEnvelopesPerFile = new List<(string file, int numFilteredEnvs)>();

                foreach (var file in DataManagement.SpectraFiles)
                {
                    int envs = file.Value.OneBasedScanToAnnotatedEnvelopes.Sum(p => p.Value.Count);
                    numFilteredEnvelopesPerFile.Add((Path.GetFileNameWithoutExtension(file.Key), envs));
                }

                double[] values = numFilteredEnvelopesPerFile.Select(p => (double)p.numFilteredEnvs).ToArray();
                double[] positions = Enumerable.Range(0, numFilteredEnvelopesPerFile.Count).Select(p => (double)p).ToArray();
                string[] labels = numFilteredEnvelopesPerFile.Select(p => p.file).ToArray();

                var envsCountPlot = new ScottPlot.Plottable.LollipopPlot(positions, values);
                envsCountPlot.Label = @"Deconvoluted Envelopes";
                envsCountPlot.LollipopColor = GuiSettings.DeconvolutedColor;
                envsCountPlot.LollipopRadius = 10;
                plot.Add(envsCountPlot);

                plot.YAxis.Label("Count");
                plot.SetAxisLimitsY(0, numFilteredEnvelopesPerFile.Max(p => p.numFilteredEnvs) * 1.2);
                plot.XTicks(positions, labels);

                StyleDashboardPlot(plot);
            }
            catch (Exception e)
            {
                errors.Add(e.Message);
            }
        }

        public static void DrawMassDistributionsDashboardPlot(Plot plot, out List<string> errors)
        {
            plot.Clear();
            errors = new List<string>();

            try
            {
                plot.Title(@"MS1 Envelope Mass Histograms");
                var color = GuiSettings.DeconvolutedColor;

                int fileNum = 0;
                double maxMass = 0;
                foreach (var file in DataManagement.SpectraFiles)
                {
                    //TODO: decon feature could be null
                    var envelopeMasses = file.Value.OneBasedScanToAnnotatedEnvelopes.SelectMany(p => p.Value.Select(v => v.PeakMzs.First().ToMass(v.Charge))).ToArray();
                    maxMass = Math.Max(maxMass, envelopeMasses.Max());

                    // generate histogram bars
                    List<(double start, double end, int count)> histogramBins = new List<(double start, double end, int count)>();

                    for (int i = 0; i < int.MaxValue; i++)
                    {
                        double binMin = i * GuiSettings.MassHistogramBinWidth;
                        double binMax = (i + 1) * GuiSettings.MassHistogramBinWidth;

                        if (binMin > envelopeMasses.Max())
                        {
                            break;
                        }

                        int itemsInBin = envelopeMasses.Count(p => p >= binMin && p < binMax);
                        histogramBins.Add((binMin, binMax, itemsInBin));
                    }

                    double[] fractionalAbundance = histogramBins.Select(p => p.count / ((double)envelopeMasses.Length)).ToArray();
                    double[] locations = histogramBins.Select(p => (p.start + p.end) / 2.0).ToArray();

                    var bar = plot.AddBar(values: fractionalAbundance, positions: locations);
                    bar.BarWidth = GuiSettings.MassHistogramBinWidth;
                    bar.FillColor = Color.FromArgb(GuiSettings.FillAlpha, color);
                    bar.BorderLineWidth = 0;
                    bar.Orientation = ScottPlot.Orientation.Horizontal;
                    bar.ValueOffsets = locations.Select(p => (double)fileNum).ToArray();
                    bar.Label = Path.GetFileNameWithoutExtension(file.Key);

                    fileNum++;
                }

                plot.YAxis.Label("Daltons");
                plot.SetAxisLimitsY(0, maxMass * 1.2);

                double[] xPositions = Enumerable.Range(0, DataManagement.SpectraFiles.Count).Select(p => (double)p).ToArray();
                string[] xLabels = DataManagement.SpectraFiles.Select(p => Path.GetFileNameWithoutExtension(p.Key)).ToArray();
                plot.XTicks(xPositions, xLabels);

                StyleDashboardPlot(plot);
                plot.Legend(enable: false);
            }
            catch (Exception e)
            {
                errors.Add(e.Message);
            }
        }

        public static void OnSpeciesChanged()
        {

        }

        private static List<MsDataScan> GetScansInRtWindow(double rt, double rtWindow, KeyValuePair<string, CachedSpectraFileData> data)
        {
            double rtWindowHalfWidth = rtWindow / 2;

            var xic = data.Value.GetTicChromatogram();
            var firstScan = xic.First(p => p.X > rt - rtWindowHalfWidth);

            var scans = new List<MsDataScan>();
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

            return scans;
        }

        public static VLine UpdateRtIndicator(MsDataScan scan, VLine indicator, Plot plot)
        {
            if (scan == null)
            {
                return indicator;
            }

            var plottables = plot.GetPlottables();
            if (indicator == null || !plottables.Any(p => p is VLine line))
            {
                indicator = plot.AddVerticalLine(scan.RetentionTime, color: GuiSettings.RtIndicatorColor,
                    width: (float)(GuiSettings.ChartLineWidth * GuiSettings.DpiScalingX));
            }

            indicator.X = scan.RetentionTime;
            plot.Render();

            return indicator;
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

        private static List<Datum> GetSummedChargeXics(List<MsDataScan> scans, double modeMass, int z)
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

                var env = PfmXplorerUtil.DeconvolutionEngine.GetIsotopicEnvelope(scan.MassSpectrum, ind, z, peaks, temp, temp2);

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
    }
}
