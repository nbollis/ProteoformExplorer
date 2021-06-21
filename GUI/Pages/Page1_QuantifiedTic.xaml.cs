using GUI;
using GUI.Modules;
using IO.MzML;
using IO.ThermoRawFileReader;
using MassSpectrometry;
using MzLibUtil;
using ProteoformExplorerObjects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
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

        public Page1_QuantifiedTic()
        {
            InitializeComponent();
            DataListView.ItemsSource = DataLoading.LoadedSpectraFileNamesWithExtensions;
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
                var spectraFileName = (string)selectedItems[0];

                if (DataLoading.SpectraFiles.ContainsKey(spectraFileName))
                {
                    DataLoading.CurrentlySelectedFile = DataLoading.SpectraFiles.First(p => p.Key == spectraFileName);
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
