using Chemistry;
using GUI;
using MassSpectrometry;
using MzLibUtil;
using mzPlot;
using ProteoformExplorer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;

namespace ProteoformExplorer
{
    /// <summary>
    /// Interaction logic for Page2_MassHistogram.xaml
    /// </summary>
    public partial class Page2_SpeciesView : Page
    {
        private ObservableCollection<string> SelectedFiles;
        private ObservableCollection<string> LoadedSpectraFilePaths;
        private Dictionary<string, DynamicDataConnection> SpectraFiles;
        private KeyValuePair<string, DynamicDataConnection> CurrentlySelectedSpectraFile;
        private ObservableCollection<AnnotatedSpecies> ListOfAnnotatedSpecies;
        private ObservableCollection<AnnotatedSpecies> SelectableAnnotatedSpecies;
        private MsDataScan CurrentScan;
        private mzPlot.Plot XicPlot;
        private mzPlot.Plot SpectrumPlot;

        public Page2_SpeciesView(Dictionary<string, DynamicDataConnection> spectraFiles, ObservableCollection<AnnotatedSpecies> loadedAnnotatedSpecies,
            ObservableCollection<string> selectedFiles, ObservableCollection<string> loadedSpectraFilePaths)
        {
            InitializeComponent();
            ListOfAnnotatedSpecies = loadedAnnotatedSpecies;
            SelectableAnnotatedSpecies = new ObservableCollection<AnnotatedSpecies>();
            SpectraFiles = spectraFiles;
            SelectedFiles = selectedFiles;
            LoadedSpectraFilePaths = loadedSpectraFilePaths;
            DataListView.ItemsSource = LoadedSpectraFilePaths;
            selectSpectraFileButton.Click += new RoutedEventHandler(HomePage.SelectDataButton_Click);
            loadFiles.Click += new RoutedEventHandler(HomePage.LoadDataButton_Click);

            SpeciesListView.ItemsSource = SelectableAnnotatedSpecies;
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Uri("HomePage.xaml", UriKind.Relative));
        }

        private void PlotSpecies(AnnotatedSpecies species)
        {
            PlotSpeciesIsotopeXics(species, out var apexScan);
            PlotSpeciesInSpectrum(species, apexScan);
        }

        private void PlotSpeciesIsotopeXics(AnnotatedSpecies species, out MsDataScan initialScan, int? charge = null)
        {
            List<int> chargesToPlot = new List<int>();
            double modeMass;

            // get apex or precursor scan
            if (species.DeconvolutionFeature != null)
            {
                initialScan = PfmXplorerUtil.GetClosestScanToRtFromDynamicConnection(CurrentlySelectedSpectraFile, species.DeconvolutionFeature.ApexRt);
                modeMass = HomePage.DeconvolutionEngine.GetModeMassFromMonoisotopicMass(species.DeconvolutionFeature.MonoisotopicMass);
            }
            else
            {
                initialScan = CurrentlySelectedSpectraFile.Value.GetOneBasedScanFromDynamicConnection(species.Identification.OneBasedPrecursorScanNumber);
                modeMass = HomePage.DeconvolutionEngine.GetModeMassFromMonoisotopicMass(species.Identification.MonoisotopicMass);
            }

            // decide on charge to plot
            if (charge == null)
            {
                if (species.DeconvolutionFeature != null)
                {
                    int i = initialScan.OneBasedScanNumber - 1;
                    while (initialScan.MsnOrder != 1)
                    {
                        initialScan = CurrentlySelectedSpectraFile.Value.GetOneBasedScanFromDynamicConnection(i);
                        i--;
                    }

                    int zToPlot = species.DeconvolutionFeature.Charges.First();
                    double intensityOfMostIntenseCharge = 0;

                    foreach (int z in species.DeconvolutionFeature.Charges)
                    {
                        double mz = modeMass.ToMz(z);
                        int ind = initialScan.MassSpectrum.GetClosestPeakIndex(mz);

                        var env = HomePage.DeconvolutionEngine.GetIsotopicEnvelope(initialScan.MassSpectrum, ind, z,
                            new List<Deconvoluter.DeconvolutedPeak>(), new HashSet<double>(), new List<(double, double)>());

                        if (env != null)
                        {
                            double summedIntensity = env.Peaks.Sum(p => p.ExperimentalIntensity);

                            if (summedIntensity > intensityOfMostIntenseCharge)
                            {
                                zToPlot = z;
                            }
                        }
                    }

                    chargesToPlot.Add(zToPlot);
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

            // decide which isotopes to plot
            List<(double mz, int z)> peaksToMakeXicsFor = new List<(double mz, int z)>();
            foreach (var z in chargesToPlot)
            {
                double mz = modeMass.ToMz(z);
                int ind = initialScan.MassSpectrum.GetClosestPeakIndex(mz);

                var env = HomePage.DeconvolutionEngine.GetIsotopicEnvelope(initialScan.MassSpectrum, ind, z,
                    new List<Deconvoluter.DeconvolutedPeak>(), new HashSet<double>(), new List<(double, double)>());

                if (env != null)
                {
                    peaksToMakeXicsFor.AddRange(env.Peaks.Select(p => (p.ExperimentalMz, p.Charge)));
                }
                else
                {
                    peaksToMakeXicsFor.Add((mz, z));
                }
            }

            // make the plots
            for (int i = 0; i < peaksToMakeXicsFor.Count; i++)
            {
                var peak = peaksToMakeXicsFor[i];
                XicPlot = GuiFunctions.PlotSpeciesInXic(peak.mz, peak.z, HomePage.DeconvolutionEngine.PpmTolerance, initialScan.RetentionTime, 2.0, CurrentlySelectedSpectraFile,
                    XicPlot, topPlotView, i == 0);
            }
        }

        private void PlotSpeciesInSpectrum(AnnotatedSpecies species, MsDataScan scan, int? charge = null)
        {
            SpectrumPlot = GuiFunctions.PlotSpeciesInSpectrum(new List<AnnotatedSpecies> { species }, scan.OneBasedScanNumber, CurrentlySelectedSpectraFile, SpectrumPlot,
                bottomPlotView, true);
        }

        private void SpeciesListView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            AnnotatedSpecies species = ((TreeView)sender).SelectedItem as AnnotatedSpecies;

            PlotSpecies(species);
        }

        private void DataListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItems = ((ListView)sender).SelectedItems;

            if (selectedItems != null && selectedItems.Count >= 1)
            {
                var spectraFilePath = (string)selectedItems[0];

                if (SpectraFiles.ContainsKey(spectraFilePath))
                {
                    CurrentlySelectedSpectraFile = SpectraFiles.First(p => p.Key == spectraFilePath);
                }
                else
                {
                    MessageBox.Show("The spectra file " + spectraFilePath + " has not been loaded yet");
                    return;
                }
            }

            SelectableAnnotatedSpecies.Clear();
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(CurrentlySelectedSpectraFile.Key);
            foreach (AnnotatedSpecies species in ListOfAnnotatedSpecies.Where(p => p.SpectraFileNameWithoutExtension == nameWithoutExtension))
            {
                SelectableAnnotatedSpecies.Add(species);
            }
        }
    }
}