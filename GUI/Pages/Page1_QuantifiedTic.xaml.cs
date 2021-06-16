using GUI;
using GUI.Modules;
using IO.MzML;
using IO.ThermoRawFileReader;
using MassSpectrometry;
using MzLibUtil;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using ProteoformExplorerObjects;
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
        private MsDataScan CurrentScan;
        private int IntegratedAreaStart;
        private int IntegratedAreaEnd;
        private mzPlot.Plot TicPlot;
        private mzPlot.Plot SpectrumPlot;

        public Page1_QuantifiedTic()
        {
            InitializeComponent();
            DataListView.ItemsSource = DataLoading.LoadedSpectraFilePaths;
            selectSpectraFileButton.Click += new RoutedEventHandler(DataLoading.SelectDataButton_Click);
            loadFiles.Click += new RoutedEventHandler(DataLoading.LoadDataButton_Click);
        }

        public void RefreshPage()
        {
            if (DataLoading.SelectedFilePaths.Count == 0)
            {
                spectraFileNameLabel.Text = "None Selected";
            }
            else if (DataLoading.SelectedFilePaths.Count == 1)
            {
                spectraFileNameLabel.Text = DataLoading.SelectedFilePaths.First();
                spectraFileNameLabel.ToolTip = DataLoading.SelectedFilePaths.First();
            }
            else if (DataLoading.SelectedFilePaths.Count > 1)
            {
                spectraFileNameLabel.Text = "[Mouse over to view]";
                spectraFileNameLabel.ToolTip = string.Join('\n', DataLoading.SelectedFilePaths);
            }
        }

        private void DisplayTic()
        {
            if (DataLoading.CurrentlySelectedFile.Value == null)
            {
                return;
            }

            // display TIC chromatogram
            var ticChromatogram = DataLoading.CurrentlySelectedFile.Value.GetTicChromatogram();

            TicPlot = new mzPlot.LinePlot(topPlotView, ticChromatogram, OxyColors.Black, 1, seriesTitle: "TIC",
                chartTitle: Path.GetFileName(DataLoading.CurrentlySelectedFile.Key), chartSubtitle: "");

            // display identified TIC chromatogram
            if (DataLoading.AllLoadedAnnotatedSpecies.Any())
            {
                var identifiedTicChromatogram = DataLoading.CurrentlySelectedFile.Value.GetIdentifiedTicChromatogram();
                TicPlot.AddLinePlot(identifiedTicChromatogram, OxyColors.Purple, 1, seriesTitle: "Identified TIC");

                var deconvolutedTicChromatogram = DataLoading.CurrentlySelectedFile.Value.GetDeconvolutedTicChromatogram();
                TicPlot.AddLinePlot(deconvolutedTicChromatogram, OxyColors.Blue, 1, seriesTitle: "Deconvoluted TIC");
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

            if (speciesInScan != null)
            {
                GuiFunctions.PlotSpeciesInSpectrum(speciesInScan, scanNum, DataLoading.CurrentlySelectedFile, SpectrumPlot, bottomPlotView);
            }
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

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Uri("HomePage.xaml", UriKind.Relative));
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
                var spectraFilePath = (string)selectedItems[0];
                var spectraFileNameWithoutExtension = Path.GetFileNameWithoutExtension(spectraFilePath);

                if (DataLoading.SpectraFiles.ContainsKey(spectraFilePath))
                {
                    DataLoading.CurrentlySelectedFile = DataLoading.SpectraFiles.First(p => p.Key == spectraFilePath);
                }
                else
                {
                    //TODO: display an error message
                    return;
                }

                DisplayTic();
            }
        }
    }
}
