using GUI;
using GUI.Modules;
using MassSpectrometry;
using System;
using System.Drawing;
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
        private MsDataScan CurrentScan;
        private int IntegratedAreaStart;
        private int IntegratedAreaEnd;

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
            if (DataLoading.CurrentlySelectedFile.Value == null)
            {
                return;
            }

            // display TIC chromatogram
            var ticChromatogram = DataLoading.CurrentlySelectedFile.Value.GetTicChromatogram();

            topPlotView.Plot.Clear();
            topPlotView.Plot.Grid(false);

            topPlotView.Plot.AddScatterLines(
                ticChromatogram.Select(p => p.X).ToArray(),
                ticChromatogram.Select(p => p.Y.Value).ToArray(),
                Color.Black, 1f, label: "TIC");

            // display identified TIC chromatogram
            if (DataLoading.AllLoadedAnnotatedSpecies.Any())
            {
                var identifiedTicChromatogram = DataLoading.CurrentlySelectedFile.Value.GetIdentifiedTicChromatogram();

                if (identifiedTicChromatogram.Any())
                {
                    topPlotView.Plot.AddScatterLines(
                        identifiedTicChromatogram.Select(p => p.X).ToArray(),
                        identifiedTicChromatogram.Select(p => p.Y.Value).ToArray(),
                        Color.Purple, 1f, label: "Identified TIC");
                }

                var deconvolutedTicChromatogram = DataLoading.CurrentlySelectedFile.Value.GetDeconvolutedTicChromatogram();

                if (deconvolutedTicChromatogram.Any())
                {
                    topPlotView.Plot.AddScatterLines(
                        deconvolutedTicChromatogram.Select(p => p.X).ToArray(),
                        deconvolutedTicChromatogram.Select(p => p.Y.Value).ToArray(),
                        Color.Blue, 1f, label: "Deconvoluted TIC");
                }
            }
        }

        private void DisplayAnnotatedSpectrum(int scanNum)
        {
            if (DataLoading.CurrentlySelectedFile.Value == null)
            {
                return;
            }

            var scan = DataLoading.CurrentlySelectedFile.Value.GetOneBasedScan(scanNum);

            if (scan == null)
            {
                return;
            }

            CurrentScan = scan;

            var speciesInScan = DataLoading.CurrentlySelectedFile.Value.SpeciesInScan(scanNum);

            GuiFunctions.PlotSpeciesInSpectrum(speciesInScan, scanNum, DataLoading.CurrentlySelectedFile, bottomPlotView);
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

            DisplayTic();
        }

        private void openFileListViewButton_Click(object sender, RoutedEventArgs e)
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
    }
}
