using MassSpectrometry;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProteoformExplorer.Objects;
using System.Windows;
using System.Windows.Controls;

namespace ProteoformExplorer.ProteoformExplorerGUI
{
    /// <summary>
    /// Interaction logic for Page3_StackedIons.xaml
    /// </summary>
    public partial class Page3_StackedIons : Page
    {
        private ObservableCollection<INode> SelectableAnnotatedSpecies;
        private const double mmPerInch = 25.4;

        public Page3_StackedIons()
        {
            InitializeComponent();
            DataListView.ItemsSource = DataLoading.LoadedSpectraFiles;

            SelectableAnnotatedSpecies = new ObservableCollection<INode>();
            SpeciesListView.ItemsSource = SelectableAnnotatedSpecies;
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Dashboard());
        }

        private void PlotSpeciesStacked(AnnotatedSpecies species, int? charge = null)
        {
            List<int> chargesToPlot = new List<int>();
            double modeMass;
            MsDataScan initialScan;

            // get apex or precursor scan
            if (species.DeconvolutionFeature != null)
            {
                initialScan = PfmXplorerUtil.GetClosestScanToRtFromDynamicConnection(DataLoading.CurrentlySelectedFile, species.DeconvolutionFeature.ApexRt, 1);
                modeMass = PfmXplorerUtil.DeconvolutionEngine.GetModeMassFromMonoisotopicMass(species.DeconvolutionFeature.MonoisotopicMass);
            }
            else
            {
                initialScan = DataLoading.CurrentlySelectedFile.Value.GetOneBasedScan(species.Identification.OneBasedPrecursorScanNumber);
                modeMass = PfmXplorerUtil.DeconvolutionEngine.GetModeMassFromMonoisotopicMass(species.Identification.MonoisotopicMass);
            }

            // decide on charges to plot
            if (charge == null)
            {
                if (species.DeconvolutionFeature != null)
                {
                    chargesToPlot.AddRange(species.DeconvolutionFeature.Charges);
                }
                else if (species.Identification != null)
                {
                    chargesToPlot.Add(species.Identification.PrecursorChargeState);
                }
            }
            else
            {
                chargesToPlot.Add(charge.Value);
            }

            int fileNum = 0;
            foreach (var file in DataLoading.SpectraFiles)
            {
                if (charge == null)
                {
                    // plot summed charge states, one line per file
                    //TODO
                }
                else
                {
                    // plot the charge state envelope, one line per file
                    GuiFunctions.PlotSummedChargeStateXic(modeMass, charge.Value, initialScan.RetentionTime, GuiSettings.ExtractionWindow, file, topPlotView,
                        clearOldPlot: fileNum == 0, label: file.Key, rtIndicator: null);

                    fileNum++;
                }
            }

            // figure out offsets and replot. have to do it twice because we don't know what the max intensity is until we get the data,
            // so it's hard to scale/offset the plots correctly until it's already plotted
            PresentationSource source = PresentationSource.FromVisual(topPlotView);

            double dpiX = 0;
            double dpiY = 0;
            if (source != null)
            {
                dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
                dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;
            }

            double inchOffset = GuiSettings.WaterfallSpacingInMm / mmPerInch;
            var axisLimits = topPlotView.Plot.GetAxisLimits(topPlotView.Plot.XAxis.AxisIndex, topPlotView.Plot.YAxis.AxisIndex);

            // y axis offset
            double yAxisRange = topPlotView.Plot.GetPixelY(axisLimits.YMax) - topPlotView.Plot.GetPixelY(axisLimits.YMin);
            double yUnitsPerDot = (axisLimits.YMax - axisLimits.YMin) / yAxisRange;
            double offsetUnitStepY = inchOffset * dpiY * yUnitsPerDot;

            // x axis offset
            double xAxisRange = topPlotView.Plot.GetPixelX(axisLimits.XMax) - topPlotView.Plot.GetPixelX(axisLimits.XMin);
            double xUnitsPerDot = (axisLimits.XMax - axisLimits.XMin) / xAxisRange;
            double offsetUnitStepX = inchOffset * dpiX * xUnitsPerDot;

            fileNum = 0;
            double xOffset = 0;
            double yOffset = 0;
            foreach (var file in DataLoading.SpectraFiles)
            {
                if (charge == null)
                {
                    // plot summed charge states, one line per file
                    //TODO
                    topPlotView.Plot.Clear();
                }
                else
                {
                    // plot the charge state envelope, one line per file
                    GuiFunctions.PlotSummedChargeStateXic(modeMass, charge.Value, initialScan.RetentionTime, GuiSettings.ExtractionWindow, file, topPlotView,
                        clearOldPlot: fileNum == 0, rtIndicator: null, xOffset, yOffset, fill: true, fillBaseline: yOffset, label: file.Key);

                    fileNum++;
                    xOffset -= offsetUnitStepX;
                    yOffset -= offsetUnitStepY;
                }
            }

            // zoom axes
            axisLimits = topPlotView.Plot.GetAxisLimits(topPlotView.Plot.XAxis.AxisIndex, topPlotView.Plot.YAxis.AxisIndex);
            topPlotView.Plot.SetAxisLimits(
                axisLimits.XMin - axisLimits.XSpan / 2,
                axisLimits.XMax + axisLimits.XSpan / 2,
                axisLimits.YMin - axisLimits.YSpan / 2,
                axisLimits.YMax + axisLimits.YSpan / 2);
        }

        private void SpeciesListView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            INode species = ((TreeView)sender).SelectedItem as INode;

            if (species is AnnotatedSpeciesNode annotatedSpeciesNode)
            {
                PlotSpeciesStacked(annotatedSpeciesNode.AnnotatedSpecies);
            }
            else if (species is AnnotatedSpeciesNodeSpecificCharge annotatedSpeciesNodeSpecificCharge)
            {
                PlotSpeciesStacked(annotatedSpeciesNodeSpecificCharge.AnnotatedSpecies, annotatedSpeciesNodeSpecificCharge.Charge);
            }

            GuiFunctions.OnSpeciesChanged();
        }

        private void DataListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GuiFunctions.OnSpectraFileChanged(sender, e);

            GuiFunctions.PopulateTreeViewWithSpeciesAndCharges(SelectableAnnotatedSpecies);
        }

        private void openFileListViewButton_Click(object sender, RoutedEventArgs e)
        {
            GuiFunctions.ShowOrHideSpectraFileList(DataListView, gridSplitter);
        }

        private void SpeciesListView_PreviewMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // double click doesn't actually do anything. the program seems to crash when double-clicking, so 
            // this event handler is here just to do nothing if a double-click occurs
        }
    }
}
