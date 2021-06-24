using Chemistry;
using MassSpectrometry;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProteoformExplorer.Objects;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Drawing;
using ScottPlot.Plottable;

namespace ProteoformExplorer.ProteoformExplorerGUI
{
    /// <summary>
    /// Interaction logic for Page2_SpeciesView.xaml
    /// </summary>
    public partial class Page2_SpeciesView : Page
    {
        private ObservableCollection<INode> SelectableAnnotatedSpecies;
        private MsDataScan CurrentScan;
        private AnnotatedSpecies CurrentlyDisplayedSpecies;
        private int? CurrentlyDisplayedCharge;
        private VLine CurrentRtIndicator;

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
            CurrentlyDisplayedSpecies = species;
            CurrentlyDisplayedCharge = charge;
        }

        private void PlotSpeciesIsotopeXics(AnnotatedSpecies species, out MsDataScan initialScan, int? charge = null)
        {
            List<int> chargesToPlot = new List<int>();
            double modeMass;

            // get apex or precursor scan
            if (species.DeconvolutionFeature != null)
            {
                initialScan = PfmXplorerUtil.GetClosestScanToRtFromDynamicConnection(DataLoading.CurrentlySelectedFile, species.DeconvolutionFeature.ApexRt);
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

            if (charge == null)
            {
                // plot summed isotopes, one line per charge
                for (int i = 0; i < chargesToPlot.Count; i++)
                {
                    int z = chargesToPlot[i];

                    GuiFunctions.PlotSummedChargeStateXic(modeMass, z, initialScan.RetentionTime, GuiSettings.ExtractionWindow,
                        DataLoading.CurrentlySelectedFile, topPlotView, clearOldPlot: i == 0, label: "z=" + z);
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

                    var env = PfmXplorerUtil.DeconvolutionEngine.GetIsotopicEnvelope(initialScan.MassSpectrum, ind, z,
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

                    GuiFunctions.PlotXic(peak.mz, peak.z, PfmXplorerUtil.DeconvolutionEngine.PpmTolerance, initialScan.RetentionTime, GuiSettings.ExtractionWindow,
                        DataLoading.CurrentlySelectedFile, topPlotView, i == 0, label: peak.mz.ToMass(peak.z).ToString("F2"));
                }
            }
        }

        private void PlotSpeciesInSpectrum(AnnotatedSpecies species, MsDataScan scan, int? charge = null)
        {
            GuiFunctions.PlotSpeciesInSpectrum(new HashSet<AnnotatedSpecies> { species }, scan.OneBasedScanNumber, DataLoading.CurrentlySelectedFile,
                bottomPlotView, out var scan2);

            CurrentScan = scan;

            CurrentRtIndicator = GuiFunctions.UpdateRtIndicator(scan, CurrentRtIndicator, topPlotView);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left)
            {
                if (CurrentScan != null && CurrentlyDisplayedSpecies != null)
                {
                    var previousScan = DataLoading.CurrentlySelectedFile.Value.GetOneBasedScan(CurrentScan.OneBasedScanNumber - 1);

                    if (previousScan != null)
                    {
                        PlotSpeciesInSpectrum(CurrentlyDisplayedSpecies, previousScan, CurrentlyDisplayedCharge);
                    }
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Right)
            {
                if (CurrentScan != null && CurrentlyDisplayedSpecies != null)
                {
                    var nextScan = DataLoading.CurrentlySelectedFile.Value.GetOneBasedScan(CurrentScan.OneBasedScanNumber + 1);

                    if (nextScan != null)
                    {
                        PlotSpeciesInSpectrum(CurrentlyDisplayedSpecies, nextScan, CurrentlyDisplayedCharge);
                    }
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

            PlotSpeciesInSpectrum(CurrentlyDisplayedSpecies, theScan, CurrentlyDisplayedCharge);
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
            GuiFunctions.SpectraFileChanged(sender, e);

            GuiFunctions.PopulateTreeViewWithSpeciesAndCharges(SelectableAnnotatedSpecies);
        }

        private void openFileListViewButton_Click(object sender, RoutedEventArgs e)
        {
            GuiFunctions.ShowOrHideSpectraFileList(DataListView, gridSplitter);
        }
    }
}