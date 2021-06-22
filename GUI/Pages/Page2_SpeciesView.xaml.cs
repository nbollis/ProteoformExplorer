using Chemistry;
using GUI;
using GUI.Modules;
using MassSpectrometry;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ProteoformExplorer
{
    /// <summary>
    /// Interaction logic for Page2_SpeciesView.xaml
    /// </summary>
    public partial class Page2_SpeciesView : Page
    {
        private ObservableCollection<INode> SelectableAnnotatedSpecies;

        public Page2_SpeciesView()
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

        private void PlotSpecies(AnnotatedSpecies species, int? charge = null)
        {
            PlotSpeciesIsotopeXics(species, out var apexScan, charge);
            PlotSpeciesInSpectrum(species, apexScan, charge);
        }

        private void PlotSpeciesIsotopeXics(AnnotatedSpecies species, out MsDataScan initialScan, int? charge = null)
        {
            List<int> chargesToPlot = new List<int>();
            double modeMass;

            // get apex or precursor scan
            if (species.DeconvolutionFeature != null)
            {
                initialScan = PfmXplorerUtil.GetClosestScanToRtFromDynamicConnection(DataLoading.CurrentlySelectedFile, species.DeconvolutionFeature.ApexRt);
                modeMass = Dashboard.DeconvolutionEngine.GetModeMassFromMonoisotopicMass(species.DeconvolutionFeature.MonoisotopicMass);
            }
            else
            {
                initialScan = DataLoading.CurrentlySelectedFile.Value.GetOneBasedScan(species.Identification.OneBasedPrecursorScanNumber);
                modeMass = Dashboard.DeconvolutionEngine.GetModeMassFromMonoisotopicMass(species.Identification.MonoisotopicMass);
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

            double rtWindow = 10.0;
            if (charge == null)
            {
                // plot summed isotopes, one line per charge
                for (int i = 0; i < chargesToPlot.Count; i++)
                {
                    int z = chargesToPlot[i];
                    GuiFunctions.PlotSummedChargeStateXic(modeMass, z, initialScan.RetentionTime, rtWindow, DataLoading.CurrentlySelectedFile, topPlotView, clearOldPlot: i == 0);
                }
            }
            else
            {
                // plot isotopes, one line per isotope

                // decide which isotopes to plot
                List<(double mz, int z)> peaksToMakeXicsFor = new List<(double mz, int z)>();
                foreach (var z in chargesToPlot)
                {
                    double mz = modeMass.ToMz(z);
                    int ind = initialScan.MassSpectrum.GetClosestPeakIndex(mz);

                    var env = Dashboard.DeconvolutionEngine.GetIsotopicEnvelope(initialScan.MassSpectrum, ind, z,
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
                    GuiFunctions.PlotXic(peak.mz, peak.z, Dashboard.DeconvolutionEngine.PpmTolerance, initialScan.RetentionTime, rtWindow, DataLoading.CurrentlySelectedFile,
                        topPlotView, i == 0);
                }
            }
        }

        private void PlotSpeciesInSpectrum(AnnotatedSpecies species, MsDataScan scan, int? charge = null)
        {
            GuiFunctions.PlotSpeciesInSpectrum(new HashSet<AnnotatedSpecies> { species }, scan.OneBasedScanNumber, DataLoading.CurrentlySelectedFile,
                bottomPlotView);
        }

        private void SpeciesListView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            INode species = ((TreeView)sender).SelectedItem as INode;

            if (species is AnnotatedSpeciesNode annotatedSpeciesNode)
            {
                PlotSpecies(annotatedSpeciesNode.AnnotatedSpecies);
            }
            else if (species is AnnotatedSpeciesNodeSpecificCharge annotatedSpeciesNodeSpecificCharge)
            {
                PlotSpecies(annotatedSpeciesNodeSpecificCharge.AnnotatedSpecies, annotatedSpeciesNodeSpecificCharge.Charge);
            }
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


    public interface INode
    {
        string Name { get; }
    }

    public class AnnotatedSpeciesNode : INode
    {
        public AnnotatedSpecies AnnotatedSpecies;
        public ObservableCollection<INode> Charges { get; set; }
        public string Name { get; }

        public AnnotatedSpeciesNode(AnnotatedSpecies species)
        {
            Charges = new ObservableCollection<INode>();
            Name = species.SpeciesLabel;
            AnnotatedSpecies = species;
        }
    }

    public class AnnotatedSpeciesNodeSpecificCharge : INode
    {
        public AnnotatedSpecies AnnotatedSpecies { get; set; }
        public int Charge { get; set; }
        public string Name { get; set; }

        public AnnotatedSpeciesNodeSpecificCharge(AnnotatedSpecies species, int charge, string name = null)
        {
            AnnotatedSpecies = species;
            Charge = charge;
            Name = "z=" + charge.ToString();
        }
    }
}