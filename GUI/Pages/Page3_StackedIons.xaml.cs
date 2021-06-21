using GUI;
using GUI.Modules;
using MassSpectrometry;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ProteoformExplorer
{
    /// <summary>
    /// Interaction logic for Page3_StackedIons.xaml
    /// </summary>
    public partial class Page3_StackedIons : Page
    {
        private ObservableCollection<INode> SelectableAnnotatedSpecies;

        public Page3_StackedIons()
        {
            InitializeComponent();
            DataListView.ItemsSource = DataLoading.LoadedSpectraFilePaths;
            selectSpectraFileButton.Click += new RoutedEventHandler(DataLoading.SelectDataButton_Click);
            loadFiles.Click += new RoutedEventHandler(DataLoading.LoadDataButton_Click);

            SelectableAnnotatedSpecies = new ObservableCollection<INode>();
            SpeciesListView.ItemsSource = SelectableAnnotatedSpecies;
        }

        private void PlotSpecies(AnnotatedSpecies species, int? charge = null)
        {
            PlotSpeciesStacked(species, charge);
        }

        private void PlotSpeciesStacked(AnnotatedSpecies species, int? charge = null)
        {
            List<int> chargesToPlot = new List<int>();
            double modeMass;
            MsDataScan initialScan;

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

            double rtWindow = 5.0;

            int fileNum = 0;
            double xOffset = 0;
            double yOffset = 0;
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
                    GuiFunctions.PlotSummedChargeStateXic(modeMass, charge.Value, initialScan.RetentionTime, rtWindow, file, topPlotView,
                        clearOldPlot: fileNum == 0, xOffset, yOffset);

                    fileNum++;
                    xOffset -= 0.2;
                    yOffset -= 1e4;
                }
            }
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
                var spectraFilePath = (string)selectedItems[0];

                if (DataLoading.SpectraFiles.ContainsKey(spectraFilePath))
                {
                    DataLoading.CurrentlySelectedFile = DataLoading.SpectraFiles.First(p => p.Key == spectraFilePath);
                }
                else
                {
                    MessageBox.Show("The spectra file " + spectraFilePath + " has not been loaded yet");
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
    }
}
