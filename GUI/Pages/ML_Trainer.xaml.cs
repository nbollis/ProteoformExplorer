using Deconvoluter;
using MassSpectrometry;
using ProteoformExplorer;
using ProteoformExplorerObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UsefulProteomicsDatabases;

namespace GUI.Pages
{
    /// <summary>
    /// Interaction logic for ML_Trainer.xaml
    /// </summary>
    public partial class ML_Trainer : Page
    {
        private List<string> mlCategories;
        private MsDataScan currentlyDisplayedScan;
        private List<DeconvolutedEnvelope> envelopeCandidates;
        private Random r = new Random();

        public ML_Trainer()
        {
            InitializeComponent();

            mlCategories = new List<string> { "charge too high", "charge too low", "correct envelope", "incomplete envelope", "noise" };
            InitializeButtons();
            DisplayDeconvolutionTrainingData();
        }

        private void InitializeButtons()
        {
            foreach (var category in mlCategories)
            {
                var button = new Button();
                button.Content = category;
                button.Name = @"button_" + category.Replace(' ', '_');
                button.Width = 150;
                button.Height = 150;
                button.Click += new RoutedEventHandler(Ml_Category_Click);

                mlCategoryButtons.Children.Add(button);
            }
        }

        private void DisplayDeconvolutionTrainingData()
        {
            Loaders.LoadElements();
            Dashboard.DeconvolutionEngine = new Deconvoluter.DeconvolutionEngine(600, 0.3, 4, 0.3, 3, 5, 2, 60, 2);

            var file = @"C:\Data\LVS_TD_Yeast\05-26-17_B7A_yeast_td_fract7_rep1.raw";

            var data = new KeyValuePair<string, CachedSpectraFileData>(
                file,
                new CachedSpectraFileData(new KeyValuePair<string, MassSpectrometry.DynamicDataConnection>(file, new IO.ThermoRawFileReader.ThermoDynamicData(file)))
            );

            int len = PfmXplorerUtil.GetLastOneBasedScanNumber(data);

            int scanNum = r.Next(1, len + 1);

            MsDataScan scan = null;

            while (scan == null || scan.MsnOrder != 1)
            {
                scanNum = r.Next(1, len + 1);
                scan = data.Value.GetOneBasedScan(scanNum);
            }

            var decon = Dashboard.DeconvolutionEngine.GetEnvelopeCandidates(scan.MassSpectrum, scan.ScanWindowRange).OrderBy(p => r.Next()).ToList();


            currentlyDisplayedScan = scan;
            envelopeCandidates = decon;
            DisplayNextEnvelope();
        }

        private void Ml_Category_Click(object sender, RoutedEventArgs e)
        {
            DisplayNextEnvelope();
        }

        private void DisplayNextEnvelope()
        {
            spectrumPlot.Plot.Clear();
            spectrumPlot.Plot.Grid(false);

            DeconvolutedEnvelope env = envelopeCandidates[r.Next(0, envelopeCandidates.Count)];

            for (int i = 0; i < currentlyDisplayedScan.MassSpectrum.XArray.Length; i++)
            {
                double mz = currentlyDisplayedScan.MassSpectrum.XArray[i];
                double intensity = currentlyDisplayedScan.MassSpectrum.YArray[i];

                spectrumPlot.Plot.AddLine(mz, 0, mz, intensity, System.Drawing.Color.Gray, 1);
            }

            foreach (var peak in env.Peaks)
            {
                int index = currentlyDisplayedScan.MassSpectrum.GetClosestPeakIndex(peak.ExperimentalMz);

                double mz = currentlyDisplayedScan.MassSpectrum.XArray[index];
                double intensity = currentlyDisplayedScan.MassSpectrum.YArray[index];

                spectrumPlot.Plot.AddLine(mz, 0, mz, intensity, System.Drawing.Color.Blue, 2);
            }

            double xZoom = (currentlyDisplayedScan.MassSpectrum.XArray.Max() - currentlyDisplayedScan.MassSpectrum.XArray.Min()) / 3;
            double yZoom = currentlyDisplayedScan.MassSpectrum.YArray.Max() / env.Peaks.Max(p => p.ExperimentalIntensity);

            spectrumPlot.Plot.AxisZoom(xZoom, yZoom, env.Peaks.Average(p => p.ExperimentalMz), 0, spectrumPlot.Plot.XAxis.AxisIndex, spectrumPlot.Plot.YAxis.AxisIndex);
            spectrumPlot.Plot.YAxis.TickLabelNotation(multiplier: true);
        }
    }
}
