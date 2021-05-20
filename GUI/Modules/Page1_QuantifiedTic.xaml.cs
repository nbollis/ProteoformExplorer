using IO.MzML;
using IO.ThermoRawFileReader;
using MassSpectrometry;
using MzLibUtil;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProteoformExplorer
{
    /// <summary>
    /// Interaction logic for Page1_QuantifiedTic.xaml
    /// </summary>
    public partial class Page1_QuantifiedTic : Page
    {
        private ObservableCollection<string> SelectedFiles;
        private ObservableCollection<string> LoadedSpectraFilePaths;
        private Dictionary<string, DynamicDataConnection> SpectraFiles;
        private KeyValuePair<string, DynamicDataConnection> CurrentlySelectedSpectraFile;
        private ObservableCollection<AnnotatedSpecies> LoadedAnnotatedSpecies;
        private Dictionary<int, List<AnnotatedEnvelope>> ScanNumberToAnnotatedEnvelopes;
        private MsDataScan CurrentScan;
        private int IntegratedAreaStart;
        private int IntegratedAreaEnd;
        private mzPlot.Plot TicPlot;
        private mzPlot.Plot SpectrumPlot;

        public Page1_QuantifiedTic(Dictionary<string, DynamicDataConnection> spectraFiles, ObservableCollection<AnnotatedSpecies> loadedAnnotatedSpecies,
            ObservableCollection<string> selectedFiles, ObservableCollection<string> loadedSpectraFilePaths)
        {
            InitializeComponent();
            LoadedAnnotatedSpecies = loadedAnnotatedSpecies;
            SpectraFiles = spectraFiles;
            SelectedFiles = selectedFiles;
            LoadedSpectraFilePaths = loadedSpectraFilePaths;
            DataListView.ItemsSource = LoadedSpectraFilePaths;
            selectSpectraFileButton.Click += new RoutedEventHandler(HomePage.SelectDataButton_Click);
            loadFiles.Click += new RoutedEventHandler(HomePage.LoadDataButton_Click);
        }

        public void RefreshPage()
        {
            if (SelectedFiles.Count == 0)
            {
                spectraFileNameLabel.Text = "None Selected";
            }
            else if (SelectedFiles.Count == 1)
            {
                spectraFileNameLabel.Text = SelectedFiles.First();
                spectraFileNameLabel.ToolTip = SelectedFiles.First();
            }
            else if (SelectedFiles.Count > 1)
            {
                spectraFileNameLabel.Text = "[Mouse over to view]";
                spectraFileNameLabel.ToolTip = string.Join('\n', SelectedFiles);
            }
        }

        private void DisplayTic()
        {
            if (CurrentlySelectedSpectraFile.Value == null)
            {
                return;
            }

            // display TIC over RT
            List<Datum> tic = new List<Datum>();
            List<Datum> identifiedTic = new List<Datum>();
            HashSet<double> mzs = new HashSet<double>();

            int scanNum = 1;
            var scan = CurrentlySelectedSpectraFile.Value.GetOneBasedScanFromDynamicConnection(scanNum);

            while (scan != null)
            {
                if (scan.MsnOrder == 1)
                {
                    // calculate TIC
                    tic.Add(new Datum(scan.RetentionTime, scan.TotalIonCurrent));

                    // calculate TIC identified
                    double identifiedScanTic = 0;
                    if (ScanNumberToAnnotatedEnvelopes.TryGetValue(scan.OneBasedScanNumber, out var envs))
                    {
                        mzs.Clear();

                        foreach (var env in envs)
                        {
                            foreach (var peak in env.PeakMzs)
                            {
                                if (!mzs.Contains(peak))
                                {
                                    mzs.Add(peak);
                                    identifiedScanTic += scan.MassSpectrum.YArray[scan.MassSpectrum.GetClosestPeakIndex(peak)];
                                }
                            }
                        }
                    }

                    identifiedTic.Add(new Datum(scan.RetentionTime, identifiedScanTic));
                }

                scanNum++;
                scan = CurrentlySelectedSpectraFile.Value.GetOneBasedScanFromDynamicConnection(scanNum);
            }

            TicPlot = new mzPlot.LinePlot(topPlotView, tic, OxyColors.Black, 1, seriesTitle: "TIC",
                chartTitle: Path.GetFileName(CurrentlySelectedSpectraFile.Key), chartSubtitle: "");

            if (LoadedAnnotatedSpecies.Any())
            {
                TicPlot.AddLinePlot(identifiedTic, OxyColors.Blue, 1, seriesTitle: "Identified TIC");
            }
        }

        private void DisplayAnnotatedSpectrum(int scanNum)
        {
            if (CurrentlySelectedSpectraFile.Value == null)
            {
                return;
            }

            var scan = CurrentlySelectedSpectraFile.Value.GetOneBasedScanFromDynamicConnection(scanNum);

            if (scan == null)
            {
                return;
            }

            CurrentScan = scan;

            ClearTicChartAnnotations();

            //List<MsDataScan> childScans = new List<MsDataScan>();

            //for (int i = 1; i < 100; i++)
            //{
            //    if (scanNum + i > SpectraFile.NumSpectra)
            //    {
            //        break;
            //    }

            //    var nextScan = SpectraFile.GetOneBasedScan(scanNum + i);
            //    if (nextScan.MsnOrder == CurrentScan.MsnOrder)
            //    {
            //        break;
            //    }
            //    childScans.Add(nextScan);
            //}

            List<Datum> spectrum = new List<Datum>();
            for (int i = 0; i < scan.MassSpectrum.XArray.Length; i++)
            {
                spectrum.Add(new Datum(scan.MassSpectrum.XArray[i], scan.MassSpectrum.YArray[i]));
            }

            SpectrumPlot = new mzPlot.SpectrumPlot(bottomPlotView, spectrum, chartTitle: "",
                chartSubtitle: "RT: " + scan.RetentionTime.ToString("F3") + "; Scan #" + scan.OneBasedScanNumber + "; MS" + scan.MsnOrder);

            SpectrumPlot.Model.Axes.First(p => p.Position == AxisPosition.Left).AbsoluteMinimum = 0;
            SpectrumPlot.Model.Axes.First(p => p.Position == AxisPosition.Bottom).AbsoluteMinimum = 0;
            SpectrumPlot.Model.Axes.First(p => p.Position == AxisPosition.Bottom).AbsoluteMaximum = scan.MassSpectrum.Range.Maximum * 1.5;

            //if (scan.MsnOrder == 1)
            //{
            var colors = SpectrumPlot.Model.DefaultColors.ToList();
            int colorIndex = 0;
            string fileNameNoExt = Path.GetFileNameWithoutExtension(CurrentlySelectedSpectraFile.Key);

            if (LoadedAnnotatedSpecies != null)
            {
                foreach (var species in LoadedAnnotatedSpecies.Where(p => p.DeconvolutionFeature != null && p.DeconvolutionFeature.SpectraFileNameWithoutExtension == fileNameNoExt))
                {
                    List<Datum> peakData = new List<Datum>();

                    foreach (var envelope in species.DeconvolutionFeature.AnnotatedEnvelopes.Where(p => p.OneBasedScanNumber == scan.OneBasedScanNumber))
                    {
                        foreach (double mz in envelope.PeakMzs)
                        {
                            int ind = scan.MassSpectrum.GetClosestPeakIndex(mz);
                            peakData.Add(new Datum(scan.MassSpectrum.XArray[ind], scan.MassSpectrum.YArray[ind]));
                        }
                    }

                    SpectrumPlot.AddSpectrumPlot(peakData, colors[colorIndex], 1.5);

                    colorIndex++;
                    if (colorIndex >= colors.Count)
                    {
                        colorIndex = 0;
                    }
                }
            }

            //var annot = new LineAnnotation();
            //annot.Color = OxyColors.Red;
            //annot.StrokeThickness = 1.5;
            //annot.X = scan.RetentionTime;
            //annot.Type = LineAnnotationType.Vertical;
            //annot.LineStyle = LineStyle.Solid;

            //TicPlot.Model.Annotations.Add(annot);

            //foreach (var childScan in childScans)
            //{
            //    if (childScan.IsolationRange != null)
            //    {
            //        var isolationAnnotBegin = new LineAnnotation();
            //        isolationAnnotBegin.Color = OxyColors.Blue;
            //        isolationAnnotBegin.StrokeThickness = 1.5;
            //        isolationAnnotBegin.X = childScan.IsolationRange.Minimum;
            //        isolationAnnotBegin.Type = LineAnnotationType.Vertical;
            //        isolationAnnotBegin.LineStyle = LineStyle.Dash;
            //        isolationAnnotBegin.Text = "#" + childScan.OneBasedScanNumber;
            //        isolationAnnotBegin.Tag = new DataTag(TagType.ChildScan, isolationAnnotBegin.Text);

            //        SpectrumPlot.Model.Annotations.Add(isolationAnnotBegin);

            //        var isolationAnnotEnd = new LineAnnotation();
            //        isolationAnnotEnd.Color = OxyColors.Blue;
            //        isolationAnnotEnd.StrokeThickness = 1.5;
            //        isolationAnnotEnd.X = childScan.IsolationRange.Maximum;
            //        isolationAnnotEnd.Type = LineAnnotationType.Vertical;
            //        isolationAnnotEnd.LineStyle = LineStyle.Dash;
            //        isolationAnnotEnd.Tag = new DataTag(TagType.ChildScan, isolationAnnotBegin.Text);

            //        SpectrumPlot.Model.Annotations.Add(isolationAnnotEnd);
            //    }
            //}

            TicPlot.RefreshChart();
            SpectrumPlot.RefreshChart();
        }

        private void ClearTicChartAnnotations()
        {
            TicPlot.Model.Annotations.Clear();

            List<Series> seriesToRemove = new List<Series>();
            foreach (var series in TicPlot.Model.Series)
            {
                if (series.Title != "TIC" && series.Title != "Identified TIC")
                {
                    seriesToRemove.Add(series);
                }
            }

            foreach (var series in seriesToRemove)
            {
                TicPlot.Model.Series.Remove(series);
            }

            TicPlot.RefreshChart();
        }

        //private double GetIdentifiedTicInScan(int oneBasedScan)
        //{
        //    double tic = double.NaN;

        //if (Annotations.Any())
        //{
        //    tic = 0;

        //    foreach (var env in GetAnnotatedEnvelopesInScan(oneBasedScan))
        //    {
        //        tic += env.SumOfPeakIntensities;
        //    }
        //}

        //   return tic;
        //}

        //private IEnumerable<AnnotatedEnvelope> GetAnnotatedEnvelopesInScan(int oneBasedScan)
        //{
        //    foreach (var env in LoadedAnnotatedSpecies
        //            .SelectMany(p => p.AnnotatedEnvelopes)
        //            .Where(p => p.OneBasedScanNumber == oneBasedScan))
        //    {
        //        yield return env;
        //    }
        //}

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left)
            {
                if (CurrentScan != null)
                {
                    DisplayAnnotatedSpectrum(CurrentScan.OneBasedScanNumber - 1);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Right)
            {
                if (CurrentScan != null)
                {
                    DisplayAnnotatedSpectrum(CurrentScan.OneBasedScanNumber + 1);
                }
                e.Handled = true;
            }
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Uri("HomePage.xaml", UriKind.Relative));
        }

        private void bottomPlotView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //    if (CurrentScan == null)
            //    {
            //        return;
            //    }

            //    SpectrumPlot.Model.Annotations.Clear();

            //    var clickedMz = TicTacUtil.GetXPositionFromMouseClickOnChart(sender, e);
            //    int index = CurrentScan.MassSpectrum.GetClosestPeakIndex(clickedMz);
            //    var mz = CurrentScan.MassSpectrum.XArray[index];
            //    var intensity = CurrentScan.MassSpectrum.YArray[index];

            //    if (Annotations.Any())
            //    {
            //        double closestAnnotatedMz = GetAnnotatedEnvelopesInScan(CurrentScan.OneBasedScanNumber)
            //            .SelectMany(p => p.PeakMzs)
            //            .OrderBy(p => Math.Abs(p - mz))
            //            .FirstOrDefault();

            //        double annotatedMzInSpectrum = CurrentScan.MassSpectrum.GetClosestPeakXvalue(closestAnnotatedMz).Value;
            //        if (annotatedMzInSpectrum == mz)
            //        {
            //            SelectedEnvelope = GetAnnotatedEnvelopesInScan(CurrentScan.OneBasedScanNumber).First(p => p.PeakMzs.Contains(closestAnnotatedMz));
            //        }

            //        //.FirstOrDefault(p => p.PeakMzs.Contains(mz));

            //    }

            //    var annot = new TextAnnotation();
            //    annot.Text = SelectedEnvelope == null ? mz.ToString("F3") : mz.ToString("F3") + "\nz=" + SelectedEnvelope.Charge;
            //    annot.TextPosition = new DataPoint(mz, intensity);
            //    SpectrumPlot.Model.Annotations.Add(annot);

            //    SpectrumPlot.RefreshChart();
        }

        private void topPlotView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //if (SpectraFile == null)
            //{
            //    return;
            //}

            //double x = TicTacUtil.GetXPositionFromMouseClickOnChart(sender, e);
            //int scanNum = SpectraFile.GetClosestOneBasedSpectrumNumber(x);

            //IntegratedAreaEnd = scanNum;

            //// calculate % TIC identified
            //var rtTic = new List<Datum>();
            //var quantifiedTic = new List<Datum>();
            //var ms1Scans = SpectraFile.GetMS1Scans().ToList();

            //foreach (var ms1Scan in ms1Scans.Where(p => p.OneBasedScanNumber >= IntegratedAreaStart && p.OneBasedScanNumber <= IntegratedAreaEnd))
            //{
            //    rtTic.Add(new Datum(ms1Scan.RetentionTime, ms1Scan.TotalIonCurrent));
            //    quantifiedTic.Add(new Datum(ms1Scan.RetentionTime, GetIdentifiedTicInScan(ms1Scan.OneBasedScanNumber)));
            //}

            //if (IntegratedAreaStart != IntegratedAreaEnd)
            //{
            //    ClearTicChartAnnotations();

            //    // highlight TIC area
            //    var areaSeries = new AreaSeries();
            //    foreach (var item in rtTic)
            //    {
            //        areaSeries.Points.Add(new DataPoint(item.X, item.Y.Value));
            //    }
            //    areaSeries.Color = OxyColors.Black;
            //    areaSeries.Color2 = OxyColors.Black;
            //    TicPlot.Model.Series.Add(areaSeries);
            //    TicPlot.RefreshChart();

            //    // highlight quantified TIC area
            //    if (quantifiedTic.Any())
            //    {
            //        var areaSeries2 = new AreaSeries();
            //        foreach (var item in quantifiedTic)
            //        {
            //            areaSeries2.Points.Add(new DataPoint(item.X, item.Y.Value));
            //        }
            //        areaSeries2.Color = OxyColors.Blue;
            //        areaSeries2.Color2 = OxyColors.Blue;
            //        TicPlot.Model.Series.Add(areaSeries2);
            //        TicPlot.RefreshChart();
            //    }
            //}
        }

        private void topPlotView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (CurrentlySelectedSpectraFile.Value == null)
            {
                return;
            }

            double rt = PfmXplorerUtil.GetXPositionFromMouseClickOnChart(sender, e);
            var theScan = PfmXplorerUtil.GetClosestScanToRtFromDynamicConnection(CurrentlySelectedSpectraFile, rt);
            DisplayAnnotatedSpectrum(theScan.OneBasedScanNumber);

            IntegratedAreaStart = theScan.OneBasedScanNumber;
        }

        private void DataListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItems = ((ListView)sender).SelectedItems;

            if (selectedItems != null && selectedItems.Count >= 1)
            {
                var spectraFilePath = (string)selectedItems[0];
                var spectraFileNameWithoutExtension = Path.GetFileNameWithoutExtension(spectraFilePath);

                if (SpectraFiles.ContainsKey(spectraFilePath))
                {
                    CurrentlySelectedSpectraFile = SpectraFiles.First(p => p.Key == spectraFilePath);
                }
                else
                {
                    //TODO: display an error message
                    return;
                }

                ScanNumberToAnnotatedEnvelopes = new Dictionary<int, List<AnnotatedEnvelope>>();

                foreach (AnnotatedSpecies item in LoadedAnnotatedSpecies.Where(p => p.DeconvolutionFeature != null
                    && p.DeconvolutionFeature.SpectraFileNameWithoutExtension == spectraFileNameWithoutExtension))
                {
                    item.DeconvolutionFeature.PopulateAnnotatedEnvelopes(CurrentlySelectedSpectraFile);

                    foreach (var env in item.DeconvolutionFeature.AnnotatedEnvelopes)
                    {
                        if (!ScanNumberToAnnotatedEnvelopes.ContainsKey(env.OneBasedScanNumber))
                        {
                            ScanNumberToAnnotatedEnvelopes.Add(env.OneBasedScanNumber, new List<AnnotatedEnvelope>());
                        }

                        ScanNumberToAnnotatedEnvelopes[env.OneBasedScanNumber].Add(env);
                    }
                    //TODO: run FlashLFQ? for identified but no decon feature species?
                }

                DisplayTic();
            }
        }
    }
}
