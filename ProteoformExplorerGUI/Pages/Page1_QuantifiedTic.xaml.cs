using MassSpectrometry;
using ProteoformExplorer.Objects;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScottPlot.Plottable;

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

        public Page1_QuantifiedTic()
        {
            InitializeComponent();
            DataListView.ItemsSource = DataLoading.LoadedSpectraFiles;
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
    }
}
