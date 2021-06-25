using MassSpectrometry;
using ProteoformExplorer.Objects;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScottPlot.Plottable;
using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;

namespace ProteoformExplorer.ProteoformExplorerGUI
{
    /// <summary>
    /// Interaction logic for Page1_QuantifiedTic.xaml
    /// </summary>
    public partial class Page1_QuantifiedTic : Page
    {
        private MsDataScan CurrentScan;
        private VLine IntegratedAreaStart;
        private VLine IntegratedAreaEnd;
        private VLine CurrentRtIndicator;
        private Text PercentDeconAnnotation;
        private Text PercentIdentifiedAnnotation;

        public Page1_QuantifiedTic()
        {
            InitializeComponent();
            DataListView.ItemsSource = DataLoading.LoadedSpectraFiles;

            // left click + pan is replaced by left click + integrate for this chart
            topPlotView.Configuration.LeftClickDragPan = false;

            PercentDeconAnnotation = new Text();
            PercentDeconAnnotation.Color = GuiSettings.DeconvolutedColor;
            PercentDeconAnnotation.FontSize = 16;

            PercentIdentifiedAnnotation = new Text();
            PercentIdentifiedAnnotation.Color = GuiSettings.IdentifiedColor;
            PercentIdentifiedAnnotation.FontSize = 16;
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Dashboard());
        }

        private void DisplayTic()
        {
            GuiFunctions.PlotTotalIonChromatograms(topPlotView, CurrentRtIndicator);
        }

        private void DisplayAnnotatedSpectrum(int scanNum)
        {
            var speciesInScan = DataLoading.CurrentlySelectedFile.Value.SpeciesInScan(scanNum);

            GuiFunctions.PlotSpeciesInSpectrum(speciesInScan, scanNum, DataLoading.CurrentlySelectedFile, bottomPlotView, out var scan);

            if (scan == null)
            {
                return;
            }

            CurrentScan = scan;

            CurrentRtIndicator = GuiFunctions.UpdateRtIndicator(scan, CurrentRtIndicator, topPlotView);
        }

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

        private void topPlotView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataLoading.CurrentlySelectedFile.Value == null)
            {
                return;
            }

            double rt = PfmXplorerUtil.GetXPositionFromMouseClickOnChart(sender, e);
            var theScan = PfmXplorerUtil.GetClosestScanToRtFromDynamicConnection(DataLoading.CurrentlySelectedFile, rt);
            DisplayAnnotatedSpectrum(theScan.OneBasedScanNumber);

            IntegratedAreaStart = new VLine();
            IntegratedAreaStart.X = theScan.RetentionTime;

            ClearIntegratedAreasAndTextAnnotations();
        }

        private void DataListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GuiFunctions.OnSpectraFileChanged(sender, e);

            DisplayTic();
        }

        private void openFileListViewButton_Click(object sender, RoutedEventArgs e)
        {
            GuiFunctions.ShowOrHideSpectraFileList(DataListView, gridSplitter);
        }

        private void topPlotView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IntegratedAreaStart == null)
            {
                return;
            }

            IntegratedAreaEnd = new VLine();
            var loc = PfmXplorerUtil.GetXPositionFromMouseClickOnChart(sender, e);
            IntegratedAreaEnd.X = loc;

            // this is in case the user dragged right-to-left instead of left-to-right
            double integratedAreaStart = Math.Min(IntegratedAreaStart.X, IntegratedAreaEnd.X);
            double integratedAreaEnd = Math.Max(IntegratedAreaStart.X, IntegratedAreaEnd.X);

            // shade the area
            if (Math.Abs(integratedAreaEnd - integratedAreaStart) > 0.001)
            {
                List<(string label, double area, double xLoc, double yLoc)> integrations = new List<(string, double, double, double)>();

                foreach (var item in topPlotView.Plot.GetPlottables())
                {
                    if (item is ScatterPlot scatter)
                    {
                        double area = 0;

                        var pointsX = scatter.Xs;
                        var pointsY = scatter.Ys;

                        int startInd = Array.IndexOf(pointsX, pointsX.First(p => p >= integratedAreaStart));
                        if (startInd > 0 && Math.Abs(pointsX[startInd - 1] - integratedAreaStart) < Math.Abs(pointsX[startInd] - integratedAreaStart))
                        {
                            startInd--;
                        }

                        int endInd = integratedAreaEnd > pointsX.Last() ? pointsX.Length - 1 : Array.IndexOf(pointsX, pointsX.First(p => p >= integratedAreaEnd));
                        if (endInd > 0 && Math.Abs(pointsX[endInd - 1] - integratedAreaEnd) < Math.Abs(pointsX[endInd] - integratedAreaEnd))
                        {
                            endInd--;
                        }

                        if (startInd == endInd)
                        {
                            continue;
                        }

                        double[] xs = new double[endInd - startInd + 3];
                        double[] ys = new double[endInd - startInd + 3];

                        xs[0] = pointsX[startInd];
                        ys[0] = 0;

                        for (int i = startInd; i <= endInd; i++)
                        {
                            xs[i - startInd + 1] = pointsX[i];
                            ys[i - startInd + 1] = pointsY[i];
                        }

                        xs[xs.Length - 1] = pointsX[endInd];
                        ys[xs.Length - 1] = 0;

                        var color = Color.FromArgb(50, scatter.Color);
                        var poly = topPlotView.Plot.AddPolygon(xs, ys, fillColor: color);
                        area = Math.Abs(polygonArea(xs, ys));

                        integrations.Add((scatter.Label, area, xs[1], ys[1]));
                    }
                }

                if (integrations.Any(p => p.label == "Deconvoluted TIC"))
                {
                    var tic = integrations.First(p => p.label == "TIC");
                    var deconTic = integrations.First(p => p.label == "Deconvoluted TIC");
                    double percentTicDeconvoluted = deconTic.area / tic.area;

                    PercentDeconAnnotation.Label = (percentTicDeconvoluted * 100).ToString("F1") + "%";
                    PercentDeconAnnotation.X = deconTic.xLoc;
                    PercentDeconAnnotation.Y = deconTic.yLoc;

                    if (!topPlotView.Plot.GetPlottables().Contains(PercentDeconAnnotation))
                    {
                        topPlotView.Plot.Add(PercentDeconAnnotation);
                    }
                }
                else if (integrations.Any(p => p.label == "Identified TIC"))
                {
                    var tic = integrations.First(p => p.label == "TIC");
                    var identTic = integrations.First(p => p.label == "Identified TIC");
                    double percentTicIdentified = identTic.area / tic.area;

                    PercentIdentifiedAnnotation.Label = (percentTicIdentified * 100).ToString("F1") + "%";
                    PercentIdentifiedAnnotation.X = identTic.xLoc;
                    PercentIdentifiedAnnotation.Y = identTic.yLoc;

                    if (!topPlotView.Plot.GetPlottables().Contains(PercentIdentifiedAnnotation))
                    {
                        topPlotView.Plot.Add(PercentIdentifiedAnnotation);
                    }
                }
            }
        }

        // from https://www.mathopenref.com/coordpolygonarea.html
        private double polygonArea(double[] xs, double[] ys)
        {
            double area = 0;   // Accumulates area 
            int numPoints = xs.Length;
            int j = numPoints - 1;

            for (int i = 0; i < numPoints; i++)
            {
                area += (xs[j] + xs[i]) * (ys[j] - ys[i]);
                j = i;  //j is previous vertex to i
            }
            return area / 2;
        }

        private void ClearIntegratedAreasAndTextAnnotations()
        {
            topPlotView.Plot.Remove(PercentDeconAnnotation);
            topPlotView.Plot.Remove(PercentIdentifiedAnnotation);

            List<IPlottable> itemsToRemove = topPlotView.Plot.GetPlottables().Where(p => p is Polygon).ToList();

            foreach (var item in itemsToRemove)
            {
                topPlotView.Plot.Remove(item);
            }
        }
    }
}
