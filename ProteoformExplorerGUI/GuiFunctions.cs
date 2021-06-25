using Chemistry;
using Deconvoluter;
using MassSpectrometry;
using MzLibUtil;
using ProteoformExplorer.Objects;
using ScottPlot;
using ScottPlot.Drawing;
using ScottPlot.Plottable;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ProteoformExplorer.ProteoformExplorerGUI
{
    public class GuiFunctions
    {
        public static void StylePlot(WpfPlot plot)
        {
            plot.Plot.Palette = new Palette(GuiSettings.ChartColorPalette);
            plot.Plot.Grid(GuiSettings.ShowChartGrid);
            plot.Plot.XAxis.Ticks(major: true, minor: false);
            plot.Plot.YAxis.Ticks(major: true, minor: false);
            plot.Plot.Legend(location: GuiSettings.LegendLocation);
            plot.Plot.XAxis.TickMarkColor(Color.Transparent);
            plot.Plot.YAxis.TickMarkColor(Color.Transparent);

            // DPI scaling, for high-resolution monitors
            PresentationSource source = PresentationSource.FromVisual(plot);

            double dpiX = 0;
            double dpiY = 0;
            if (source != null)
            {
                dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
                dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;
            }
            else
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                {
                    dpiX = g.DpiX;
                    dpiY = g.DpiY;
                }
            }

            if (dpiX < 96 || dpiY < 96)
            {
                return;
            }

            double xScale = dpiX / 96;
            double yScale = dpiY / 96;

            GuiSettings.DpiScalingX = xScale;
            GuiSettings.DpiScalingY = yScale;

            plot.Plot.Layout();

            plot.Plot.XAxis.Line(width: (float)yScale);
            plot.Plot.XAxis.Label(size: (float)(GuiSettings.ChartLabelFontSize * yScale)
            //    , bold: true
                );
            plot.Plot.XAxis.TickLabelStyle(fontSize: (float)(GuiSettings.ChartTickFontSize * yScale)
            //    , fontBold: true
                );

            plot.Plot.YAxis.Line(width: (float)xScale);
            plot.Plot.YAxis.Label(size: (float)(GuiSettings.ChartLabelFontSize * xScale)
            //    , bold: true
                );
            plot.Plot.YAxis.TickLabelStyle(fontSize: (float)(GuiSettings.ChartTickFontSize * xScale)
            //    , fontBold: true
                );

            var legend = plot.Plot.Legend(location: GuiSettings.LegendLocation);
            legend.FontSize = (float)(GuiSettings.ChartLegendFontSize * xScale);

            // title
            plot.Plot.XAxis2.Label(size: (float)(GuiSettings.ChartHeaderFontSize * xScale));

            foreach (var item in plot.Plot.GetPlottables())
            {
                if (item is ScatterPlot scatter)
                {
                    //TODO: this will affect all lines... spectra and xic..
                    scatter.LineWidth = GuiSettings.ChartLineWidth * yScale;
                }
                else if (item is LollipopPlot lollipopPlot)
                {
                    //TODO
                    lollipopPlot.ErrorLineWidth = (float)(GuiSettings.ChartLineWidth * xScale);
                    lollipopPlot.BarWidth = (float)(GuiSettings.ChartLineWidth * xScale);
                }
            }
        }

        public static void StyleDashboardPlot(WpfPlot plot)
        {
            StylePlot(plot);

            plot.Plot.XAxis.TickLabelStyle(rotation: (float)GuiSettings.XLabelRotation);
            plot.Plot.Frame(top: false, right: false);
            plot.Plot.YAxis.Line(visible: false);
        }

        public static void StyleIonChromatogramPlot(WpfPlot plot, VLine rtIndicator)
        {
            if (rtIndicator != null)
            {
                plot.Plot.Add(rtIndicator);
            }

            StylePlot(plot);

            plot.Plot.YAxis.TickLabelNotation(multiplier: true);
            plot.Plot.YAxis.Label("Intensity");
            plot.Plot.XAxis.Label("Retention Time");
            plot.Plot.Layout(padding: 10);
        }

        public static void StyleSpectrumPlot(WpfPlot plot, MsDataScan scan, string dataFileName)
        {
            StylePlot(plot);

            try
            {
                plot.Plot.Title(dataFileName + "  Scan#" + scan.OneBasedScanNumber + "  RT: " + scan.RetentionTime + "  MS" + scan.MsnOrder + "\n" +
                    " " + scan.ScanFilter, bold: false);
            }
            catch (Exception e)
            {
                plot.Plot.Title(dataFileName + "  Scan#" + scan.OneBasedScanNumber + "  RT: " + scan.RetentionTime + "  MS" + scan.MsnOrder, bold: false);
            }

            plot.Plot.YAxis.TickLabelNotation(multiplier: true);
            plot.Plot.YAxis.Label("Intensity");
            plot.Plot.XAxis.Label("m/z");
            plot.Plot.SetViewLimits(scan.ScanWindowRange.Minimum, scan.ScanWindowRange.Maximum, 0, scan.MassSpectrum.YArray.Max() * 2.0);
        }

        public static void PlotSpeciesInSpectrum(HashSet<AnnotatedSpecies> allSpeciesToPlot, int oneBasedScan, KeyValuePair<string, CachedSpectraFileData> data,
            WpfPlot spectrumPlot, out MsDataScan scan, int? charge = null)
        {
            spectrumPlot.Plot.Clear();

            if (data.Value == null)
            {
                scan = null;
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
                spectrumPlot.Plot.AddLine(item.X, 0, item.X, item.Y.Value, GuiSettings.UnannotatedSpectrumColor, (float)GuiSettings.ChartLineWidth);
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

                    var color = spectrumPlot.Plot.GetNextColor();

                    foreach (var item in annotatedData)
                    {
                        spectrumPlot.Plot.AddLine(item.X, 0, item.X, item.Y.Value, color, (float)GuiSettings.AnnotatedEnvelopeLineWidth);
                    }
                }
            }

            StyleSpectrumPlot(spectrumPlot, scan, data.Key);
            spectrumPlot.Plot.SetAxisLimits(scan.ScanWindowRange.Minimum, scan.ScanWindowRange.Maximum, 0, scan.MassSpectrum.YArray.Max() * 1.2);
        }

        public static void PlotXic(double mz, int z, Tolerance tolerance, double rt, double rtWindow, KeyValuePair<string, CachedSpectraFileData> data, WpfPlot xicPlot,
            bool clearOldPlot, VLine rtIndicator, bool fill = false, string label = null)
        {
            if (clearOldPlot)
            {
                xicPlot.Plot.Clear();
            }

            var scans = GetScansInRtWindow(rt, rtWindow, data);

            var xicData = GetXicData(scans, mz, z, tolerance);

            var xs = xicData.Select(p => p.X).ToArray();
            var ys = xicData.Select(p => p.Y.Value).ToArray();
            var color = xicPlot.Plot.GetNextColor();

            xicPlot.Plot.AddScatterLines(xs, ys, color, label: label);
            xicPlot.Plot.Legend(label != null, location: GuiSettings.LegendLocation);

            if (fill)
            {
                xicPlot.Plot.AddFill(xs, ys, color: color);
            }

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

                //xicPlot.Plot.SetViewLimits(xMin, xMax, yMin, yMax * 2);
            }

            StyleIonChromatogramPlot(xicPlot, rtIndicator);
        }

        public static void PlotSummedChargeStateXic(double modeMass, int z, double rt, double rtWindow, KeyValuePair<string, CachedSpectraFileData> data, WpfPlot xicPlot,
            bool clearOldPlot, VLine rtIndicator, double xOffset = 0, double yOffset = 0, bool fill = false, double fillBaseline = 0, string label = null)
        {
            if (clearOldPlot)
            {
                xicPlot.Plot.Clear();
            }

            var scans = GetScansInRtWindow(rt, rtWindow, data);

            var xicData = GetSummedChargeXics(scans, modeMass, z);

            var xs = xicData.Select(p => p.X + xOffset).ToArray();
            var ys = xicData.Select(p => p.Y.Value + yOffset).ToArray();
            var color = xicPlot.Plot.GetNextColor();

            xicPlot.Plot.AddScatterLines(xs, ys, color, lineWidth: (float)GuiSettings.ChartLineWidth, label: label);
            xicPlot.Plot.Legend(label != null, location: GuiSettings.LegendLocation);

            if (fill)
            {
                var colorWithTransparency = Color.FromArgb(190, color.R, color.G, color.B);
                xicPlot.Plot.AddFill(xs, ys, baseline: fillBaseline, color: colorWithTransparency);
            }

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

                //xicPlot.Plot.SetViewLimits(xMin, xMax, yMin, yMax * 2);
            }

            StyleIonChromatogramPlot(xicPlot, rtIndicator);
        }

        public static void PlotTotalIonChromatograms(WpfPlot plot, VLine rtIndicator)
        {
            plot.Plot.Clear();

            if (DataLoading.CurrentlySelectedFile.Value == null)
            {
                return;
            }

            // display TIC chromatogram
            var ticChromatogram = DataLoading.CurrentlySelectedFile.Value.GetTicChromatogram();

            plot.Plot.AddScatterLines(
                ticChromatogram.Select(p => p.X).ToArray(),
                ticChromatogram.Select(p => p.Y.Value).ToArray(),
                Color.Black, (float)GuiSettings.ChartLineWidth, label: "TIC");

            // display identified TIC chromatogram
            if (DataLoading.AllLoadedAnnotatedSpecies.Any())
            {
                var identifiedTicChromatogram = DataLoading.CurrentlySelectedFile.Value.GetIdentifiedTicChromatogram();

                if (identifiedTicChromatogram.Any())
                {
                    plot.Plot.AddScatterLines(
                        identifiedTicChromatogram.Select(p => p.X).ToArray(),
                        identifiedTicChromatogram.Select(p => p.Y.Value).ToArray(),
                        GuiSettings.IdentifiedColor, (float)GuiSettings.ChartLineWidth, label: "Identified TIC");
                }

                var deconvolutedTicChromatogram = DataLoading.CurrentlySelectedFile.Value.GetDeconvolutedTicChromatogram();

                if (deconvolutedTicChromatogram.Any())
                {
                    plot.Plot.AddScatterLines(
                        deconvolutedTicChromatogram.Select(p => p.X).ToArray(),
                        deconvolutedTicChromatogram.Select(p => p.Y.Value).ToArray(),
                        GuiSettings.DeconvolutedColor, (float)GuiSettings.ChartLineWidth, label: "Deconvoluted TIC");
                }
            }

            plot.Plot.SetViewLimits(xMin: ticChromatogram.Min(p => p.X), xMax: ticChromatogram.Max(p => p.X), yMin: 0, yMax: ticChromatogram.Max(p => p.Y.Value) * 2.0);

            StyleIonChromatogramPlot(plot, rtIndicator);
        }

        public static void DrawPercentTicPerFileInfoDashboardPlot(WpfPlot plot, out List<string> errors)
        {
            plot.Plot.Clear();
            errors = new List<string>();

            try
            {
                plot.Plot.Title(@"MS1 TIC");

                var ticValues = new List<(string file, double tic, double deconvolutedTic, double identifiedTic)>();

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
                ticPlot.LollipopColor = GuiSettings.TicColor;
                ticPlot.LollipopRadius = 10;
                plot.Plot.Add(ticPlot);

                var deconTicPlot = new ScottPlot.Plottable.LollipopPlot(positions, ticValues.Select(p => p.deconvolutedTic).ToArray());
                deconTicPlot.Label = @"Deconvoluted TIC";
                deconTicPlot.LollipopColor = GuiSettings.DeconvolutedColor;
                deconTicPlot.LollipopRadius = 10;
                plot.Plot.Add(deconTicPlot);

                var identTicPlot = new ScottPlot.Plottable.LollipopPlot(positions, ticValues.Select(p => p.identifiedTic).ToArray());
                identTicPlot.Label = @"Identified TIC";
                identTicPlot.LollipopColor = GuiSettings.IdentifiedColor;
                identTicPlot.LollipopRadius = 10;
                plot.Plot.Add(identTicPlot);

                plot.Plot.YAxis.Label("Intensity");
                plot.Plot.SetAxisLimitsY(ticValues.Max(p => p.tic) * -0.03, ticValues.Max(p => p.tic) * 1.4);
                plot.Plot.XTicks(positions, labels);
                plot.Plot.YAxis.TickLabelNotation(multiplier: true);

                StyleDashboardPlot(plot);
            }
            catch (Exception e)
            {
                errors.Add(e.Message);
            }
        }

        public static void DrawNumEnvelopesDashboardPlot(WpfPlot plot, out List<string> errors)
        {
            plot.Plot.Clear();
            errors = new List<string>();

            try
            {
                plot.Plot.Title(@"MS1 Envelope Counts");

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
                envsCountPlot.LollipopColor = GuiSettings.DeconvolutedColor;
                envsCountPlot.LollipopRadius = 10;
                plot.Plot.Add(envsCountPlot);

                plot.Plot.YAxis.Label("Count");
                plot.Plot.SetAxisLimitsY(0, numFilteredEnvelopesPerFile.Max(p => p.numFilteredEnvs) * 1.2);
                plot.Plot.XTicks(positions, labels);

                StyleDashboardPlot(plot);
            }
            catch (Exception e)
            {
                errors.Add(e.Message);
            }
        }

        public static void DrawMassDistributionsDashboardPlot(WpfPlot plot, out List<string> errors)
        {
            plot.Plot.Clear();
            errors = new List<string>();

            try
            {
                plot.Plot.Title(@"MS1 Envelope Mass Histograms");
                var color = GuiSettings.DeconvolutedColor;

                int fileNum = 0;
                double maxMass = 0;
                foreach (var file in DataLoading.SpectraFiles)
                {
                    //TODO: decon feature could be null
                    var envelopeMasses = file.Value.OneBasedScanToAnnotatedEnvelopes.SelectMany(p => p.Value.Select(v => v.PeakMzs.First().ToMass(v.Charge))).ToArray();
                    maxMass = Math.Max(maxMass, envelopeMasses.Max());

                    // generate histogram bars
                    List<(double start, double end, int count)> histogramBins = new List<(double start, double end, int count)>();
                    double binWidth = 500;

                    for (int i = 0; i < int.MaxValue; i++)
                    {
                        double binMin = i * binWidth;
                        double binMax = (i + 1) * binWidth;

                        if (binMin > envelopeMasses.Max())
                        {
                            break;
                        }

                        int itemsInBin = envelopeMasses.Count(p => p >= binMin && p < binMax);
                        histogramBins.Add((binMin, binMax, itemsInBin));
                    }

                    double[] fractionalAbundance = histogramBins.Select(p => p.count / ((double)envelopeMasses.Length)).ToArray();
                    double[] locations = histogramBins.Select(p => (p.start + p.end) / 2.0).ToArray();

                    var bar = plot.Plot.AddBar(values: fractionalAbundance, positions: locations);
                    bar.BarWidth = binWidth;
                    bar.FillColor = Color.FromArgb(180, color);
                    bar.BorderLineWidth = 0;
                    bar.Orientation = ScottPlot.Orientation.Horizontal;
                    bar.ValueOffsets = locations.Select(p => (double)fileNum).ToArray();
                    bar.Label = Path.GetFileNameWithoutExtension(file.Key);

                    fileNum++;
                }

                plot.Plot.YAxis.Label("Daltons");
                plot.Plot.SetAxisLimitsY(0, maxMass * 1.2);

                double[] xPositions = Enumerable.Range(0, DataLoading.SpectraFiles.Count).Select(p => (double)p).ToArray();
                string[] xLabels = DataLoading.SpectraFiles.Select(p => Path.GetFileNameWithoutExtension(p.Key)).ToArray();
                plot.Plot.XTicks(xPositions, xLabels);

                StyleDashboardPlot(plot);
                plot.Plot.Legend(enable: false);
            }
            catch (Exception e)
            {
                errors.Add(e.Message);
            }
        }

        public static List<MsDataScan> GetScansInRtWindow(double rt, double rtWindow, KeyValuePair<string, CachedSpectraFileData> data)
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

        public static void PopulateTreeViewWithSpeciesAndCharges(ObservableCollection<INode> SelectableAnnotatedSpecies)
        {
            SelectableAnnotatedSpecies.Clear();
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(DataLoading.CurrentlySelectedFile.Key);

            foreach (AnnotatedSpecies species in DataLoading.AllLoadedAnnotatedSpecies.Where(p => p.SpectraFileNameWithoutExtension == nameWithoutExtension))
            {
                var parentNode = new AnnotatedSpeciesNode(species);
                SelectableAnnotatedSpecies.Add(parentNode);

                if (species.DeconvolutionFeature != null)
                {
                    foreach (var charge in species.DeconvolutionFeature.Charges)
                    {
                        var childNode = new AnnotatedSpeciesNodeSpecificCharge(species, charge);
                        parentNode.Charges.Add(childNode);
                    }
                }
                if (species.Identification != null)
                {
                    int charge = species.Identification.PrecursorChargeState;
                    var childNode = new AnnotatedSpeciesNodeSpecificCharge(species, charge, charge.ToString() + " (ID)");
                    parentNode.Charges.Add(childNode);
                }
            }
        }

        public static void ShowOrHideSpectraFileList(ListView DataListView, GridSplitter gridSplitter)
        {
            if (DataListView.Visibility == Visibility.Collapsed)
            {
                DataListView.Visibility = Visibility.Visible;
                gridSplitter.Visibility = Visibility.Visible;
            }
            else
            {
                DataListView.Visibility = Visibility.Collapsed;
                gridSplitter.Visibility = Visibility.Hidden;
            }
        }

        public static void OnSpeciesChanged()
        {

        }

        public static void OnSpectraFileChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItems = ((ListView)sender).SelectedItems;

            if (selectedItems != null && selectedItems.Count >= 1)
            {
                var spectraFileName = ((FileForDataGrid)selectedItems[0]).FileNameWithExtension;

                if (DataLoading.SpectraFiles.ContainsKey(spectraFileName))
                {
                    DataLoading.CurrentlySelectedFile = DataLoading.SpectraFiles.First(p => p.Key == spectraFileName);
                }
                else
                {
                    MessageBox.Show("The spectra file " + spectraFileName + " has not been loaded yet");
                    return;
                }
            }
        }

        public static VLine UpdateRtIndicator(MsDataScan scan, VLine indicator, WpfPlot plot)
        {
            if (scan == null)
            {
                return indicator;
            }

            var plottables = plot.Plot.GetPlottables();
            if (indicator == null || !plottables.Any(p => p is VLine line))
            {
                indicator = plot.Plot.AddVerticalLine(scan.RetentionTime, color: Color.Red, width: 1.5f);
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
