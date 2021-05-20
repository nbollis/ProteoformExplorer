using Chemistry;
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
        private MsDataScan CurrentScan;
        private mzPlot.Plot XicPlot;
        private mzPlot.Plot SpectrumPlot;

        public Page2_SpeciesView(Dictionary<string, DynamicDataConnection> spectraFiles, ObservableCollection<AnnotatedSpecies> loadedAnnotatedSpecies,
            ObservableCollection<string> selectedFiles, ObservableCollection<string> loadedSpectraFilePaths)
        {
            InitializeComponent();
            ListOfAnnotatedSpecies = loadedAnnotatedSpecies;
            SpectraFiles = spectraFiles;
            SelectedFiles = selectedFiles;
            LoadedSpectraFilePaths = loadedSpectraFilePaths;
            DataListView.ItemsSource = LoadedSpectraFilePaths;
            selectSpectraFileButton.Click += new RoutedEventHandler(HomePage.SelectDataButton_Click);
            loadFiles.Click += new RoutedEventHandler(HomePage.LoadDataButton_Click);

            SpeciesListView.ItemsSource = ListOfAnnotatedSpecies;
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Uri("HomePage.xaml", UriKind.Relative));
        }

        private void PlotSpecies(AnnotatedSpecies species, MsDataScan scan)
        {
            PlotSpeciesIsotopeXics(species, scan);
            PlotSpeciesInSpectrum(species, scan);
        }

        private void PlotSpeciesIsotopeXics(AnnotatedSpecies species, MsDataScan scan, int? charge = null)
        {
            double rtWindowHalfWidth = 2.5;

            MsDataScan startScan = null;

            for (int i = scan.OneBasedScanNumber; i >= 1; i--)
            {
                var theScan = CurrentlySelectedSpectraFile.Value.GetOneBasedScanFromDynamicConnection(i);

                if (theScan.RetentionTime < scan.RetentionTime - rtWindowHalfWidth)
                {
                    break;
                }

                startScan = theScan;
            }

            List<MsDataScan> scans = new List<MsDataScan>();
            for (int i = startScan.OneBasedScanNumber; i < 1000000; i++)
            {
                var theScan = CurrentlySelectedSpectraFile.Value.GetOneBasedScanFromDynamicConnection(i);

                if (theScan.RetentionTime > scan.RetentionTime + rtWindowHalfWidth)
                {
                    break;
                }

                if (theScan.MsnOrder == 1)
                {
                    scans.Add(theScan);
                }
            }

            int z = species.DeconvolutionFeature.Charges[species.DeconvolutionFeature.Charges.Count / 2];
            for (int i = 0; i < 10; i++)
            {
                List<Datum> xicData = new List<Datum>();

                double isotopeMass = species.DeconvolutionFeature.MonoisotopicMass + i * Constants.C13MinusC12;
                Tolerance t = new PpmTolerance(5);

                foreach (var item in scans)
                {
                    int index = item.MassSpectrum.GetClosestPeakIndex(isotopeMass.ToMz(z));

                    if (t.Within(item.MassSpectrum.XArray[index], isotopeMass.ToMz(z)))
                    {
                        xicData.Add(new Datum(item.RetentionTime, item.MassSpectrum.YArray[index]));
                    }
                    else
                    {
                        xicData.Add(new Datum(item.RetentionTime, 0));
                    }
                }

                if (i == 0)
                {
                    XicPlot = new LinePlot(topPlotView, xicData);
                }
                else
                {
                    XicPlot.AddLinePlot(xicData);
                }
            }
        }

        private void PlotSpeciesInSpectrum(AnnotatedSpecies species, MsDataScan scan, int? charge = null)
        {
            // add non-annotated peaks
            List<Datum> spectrumData = new List<Datum>();

            for (int i = 0; i < scan.MassSpectrum.XArray.Length; i++)
            {
                spectrumData.Add(new Datum(scan.MassSpectrum.XArray[i], scan.MassSpectrum.YArray[i]));
            }

            SpectrumPlot = new SpectrumPlot(bottomPlotView, spectrumData);

            // add annotated peaks
            List<Datum> annotatedData = new List<Datum>();
            List<int> chargesToPlot = new List<int>();

            if (species.DeconvolutionFeature != null)
            {
                double mass = species.DeconvolutionFeature.MonoisotopicMass;

                if (charge == null)
                {
                    chargesToPlot.AddRange(species.DeconvolutionFeature.Charges);
                }
                else
                {
                    chargesToPlot.Add(charge.Value);
                }

                foreach (var z in chargesToPlot)
                {
                    Tolerance t = new PpmTolerance(5);

                    bool peakHasBeenObserved = false;

                    for (int i = 0; i < 20; i++)
                    {
                        double isotopeMass = (mass + i * Constants.C13MinusC12).ToMz(z);
                        int index = scan.MassSpectrum.GetClosestPeakIndex(isotopeMass);
                        double expMz = scan.MassSpectrum.XArray[index];

                        if (t.Within(expMz.ToMass(z), isotopeMass.ToMass(z)))
                        {
                            annotatedData.Add(new Datum(scan.MassSpectrum.XArray[index], scan.MassSpectrum.YArray[index]));
                            peakHasBeenObserved = true;
                        }
                        //else if (peakHasBeenObserved)
                        //{
                        //    break;
                        //}
                    }
                }
            }
            else if (species.Identification != null)
            {
                //TODO
            }

            SpectrumPlot.AddSpectrumPlot(annotatedData, OxyPlot.OxyColors.Blue, 2.0);
            ZoomAxes(annotatedData, SpectrumPlot);
        }

        protected void ZoomAxes(List<Datum> annotatedIons, Plot plot, double yZoom = 1.2)
        {
            double highestAnnotatedIntensity = 0;
            double highestAnnotatedMz = double.MinValue;
            double lowestAnnotatedMz = double.MaxValue;

            foreach (var ion in annotatedIons)
            {
                double mz = ion.X;
                double intensity = ion.Y.Value;

                highestAnnotatedIntensity = Math.Max(highestAnnotatedIntensity, intensity);
                highestAnnotatedMz = Math.Max(highestAnnotatedMz, mz);
                lowestAnnotatedMz = Math.Min(lowestAnnotatedMz, mz);
            }

            if (highestAnnotatedIntensity > 0)
            {
                plot.Model.Axes[1].Zoom(0, highestAnnotatedIntensity * yZoom);
            }

            if (highestAnnotatedMz > double.MinValue && lowestAnnotatedMz < double.MaxValue)
            {
                plot.Model.Axes[0].Zoom(lowestAnnotatedMz - 100, highestAnnotatedMz + 100);
            }
        }

        private void SpeciesListView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            AnnotatedSpecies species = ((TreeView)sender).SelectedItem as AnnotatedSpecies;
            MsDataScan scan = null;

            var file = SpectraFiles.FirstOrDefault(p => System.IO.Path.GetFileNameWithoutExtension(p.Key)
                == species.DeconvolutionFeature.SpectraFileNameWithoutExtension);

            if (file.Value == null)
            {
                MessageBox.Show("The spectra file '" + species.DeconvolutionFeature.SpectraFileNameWithoutExtension + "' has not been loaded");
            }

            CurrentlySelectedSpectraFile = file;

            for (int i = 1; i < 10000000; i++)
            {
                var theScan = file.Value.GetOneBasedScanFromDynamicConnection(i);

                if (theScan.RetentionTime >= species.DeconvolutionFeature.ApexRt && theScan.MsnOrder == 1)
                {
                    scan = theScan;
                    break;
                }
            }

            PlotSpecies(species, scan);
        }

        private void DataListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItems = ((ListView)sender).SelectedItems;

            if (selectedItems != null && selectedItems.Count >= 1)
            {
                var spectraFilePath = (string)selectedItems[0];
                var spectraFileNameWithoutExtension = Path.GetFileNameWithoutExtension(spectraFilePath);

                if (SpectraFiles.ContainsKey(spectraFilePath))
                {
                    CurrentlySelectedSpectraFile = SpectraFiles.First(p => p.Key == spectraFilePath);
                }
                else
                {
                    //TODO: display an error message
                    return;
                }
            }
        }
    }
}
