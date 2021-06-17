using Chemistry;
using Deconvoluter;
using GUI;
using GUI.Modules;
using IO.MzML;
using IO.ThermoRawFileReader;
using MassSpectrometry;
using ProteoformExplorerObjects;
using Proteomics.ProteolyticDigestion;
using ScottPlot.Drawing;
using ScottPlot.Statistics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ProteoformExplorer
{
    /// <summary>
    /// Interaction logic for Dashboard.xaml
    /// </summary>
    public partial class Dashboard : Page
    {
        private static Page1_QuantifiedTic Page1;
        private static Page2_SpeciesView Page2;
        private static Page3_StackedIons Page3;
        public static DeconvolutionEngine DeconvolutionEngine;

        public Dashboard()
        {
            InitializeComponent();
            InitializeDashboard();

            if (Page1 == null)
            {
                Page1 = new Page1_QuantifiedTic();
                Page2 = new Page2_SpeciesView();
                Page3 = new Page3_StackedIons();
            }
        }

        private void Chart1_Click(object sender, RoutedEventArgs e)
        {
            Page1.RefreshPage();
            this.NavigationService.Navigate(Page1);
        }

        private void Chart2_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(Page2);
        }

        private void Chart3_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(Page3);
        }

        private void InitializeDashboard()
        {
            DrawPercentTicInfo();
            DrawNumEnvelopes();
            DrawMassDistributions();
        }

        private void DrawPercentTicInfo()
        {
            // set color palette
            DashboardPlot1.Plot.Palette = new Palette(GuiFunctions.ColorPalette);

            List<(string file, double tic, double deconvolutedTic, double identifiedTic)> ticValues
                = new List<(string file, double tic, double deconvolutedTic, double identifiedTic)>();

            foreach (var file in DataLoading.SpectraFiles)
            {
                double tic = 0;
                double deconvolutedTic = 0;
                double identifiedTic = 0;

                var ticChromatogram = file.Value.GetTicChromatogram();
                if (ticChromatogram != null)
                {
                    tic = ticChromatogram.Sum(p => p.Y.Value);
                }

                var deconvolutedTicChromatogram = file.Value.GetDeconvolutedTicChromatogram();
                if (deconvolutedTicChromatogram != null)
                {
                    deconvolutedTic = deconvolutedTicChromatogram.Sum(p => p.Y.Value);
                }

                var identifiedTicChromatogram = file.Value.GetIdentifiedTicChromatogram();
                if (identifiedTicChromatogram != null)
                {
                    identifiedTic = identifiedTicChromatogram.Sum(p => p.Y.Value);
                }

                ticValues.Add((Path.GetFileNameWithoutExtension(file.Key), tic, deconvolutedTic, identifiedTic));
            }

            double[] positions = Enumerable.Range(0, ticValues.Count).Select(p => (double)p).ToArray();
            string[] labels = ticValues.Select(p => p.file).ToArray();

            DashboardPlot1.Plot.AddLollipop(ticValues.Select(p => p.tic).ToArray());
            DashboardPlot1.Plot.AddLollipop(ticValues.Select(p => p.deconvolutedTic).ToArray());
            DashboardPlot1.Plot.AddLollipop(ticValues.Select(p => p.identifiedTic).ToArray());

            DashboardPlot1.Plot.XTicks(positions, labels);
            DashboardPlot1.Plot.YAxis.TickLabelNotation(multiplier: true);
        }

        private void DrawNumEnvelopes()
        {
            // set color palette
            DashboardPlot2.Plot.Palette = new Palette(GuiFunctions.ColorPalette);

            var numFilteredEnvelopesPerFile = new List<(string file, int numFilteredEnvs)>();

            foreach (var file in DataLoading.SpectraFiles)
            {
                int envs = file.Value.OneBasedScanToAnnotatedEnvelopes.Sum(p => p.Value.Count);
                numFilteredEnvelopesPerFile.Add((Path.GetFileNameWithoutExtension(file.Key), envs));
            }

            double[] positions = Enumerable.Range(0, numFilteredEnvelopesPerFile.Count).Select(p => (double)p).ToArray();
            string[] labels = numFilteredEnvelopesPerFile.Select(p => p.file).ToArray();

            DashboardPlot2.Plot.AddLollipop(numFilteredEnvelopesPerFile.Select(p => (double)p.numFilteredEnvs).ToArray());

            DashboardPlot2.Plot.XTicks(positions, labels);
            DashboardPlot2.Plot.YAxis.TickLabelNotation();
        }

        private void DrawMassDistributions()
        {
            DashboardPlot3.Plot.Palette = new Palette(GuiFunctions.ColorPalette);

            var massPopulations = new List<(string file, double[] masses)>();

            int fileNum = 0;
            foreach (var file in DataLoading.SpectraFiles)
            {
                //TODO: decon feature could be null
                var envelopeMasses = file.Value.OneBasedScanToAnnotatedEnvelopes.SelectMany(p => p.Value.Select(v => v.PeakMzs.First().ToMass(v.Charge))).ToArray();

                var color = DashboardPlot3.Plot.GetNextColor();
                var hist = new Histogram(envelopeMasses, binSize: 1000);

                var bar = DashboardPlot3.Plot.AddBar(values: hist.countsFrac, positions: hist.bins);
                bar.BarWidth = hist.binSize;
                bar.FillColor = Color.FromArgb(50, color);
                bar.BorderLineWidth = 0;
                bar.Orientation = ScottPlot.Orientation.Horizontal;
                bar.ValueOffsets = hist.countsFrac.Select(p => (double)fileNum).ToArray();

                fileNum++;
            }

            


            //DashboardPlot3.Plot.AddScatterLines(ys: hist.bins, xs: hist.probability, color: Color.FromArgb(150, color), lineWidth: 3);

            //var populations = massPopulations.Select(p => p.pop).ToArray();
            //string[] labels = massPopulations.Select(p => p.file).ToArray();

            //DashboardPlot3.Plot.AddPopulations(populations);
            //DashboardPlot3.Plot.XTicks(labels);

            //var new popSeries = new PopulationSeries(populations);
            //var item = new PopulationMultiSeries();
            //DashboardPlot3.Plot.PlotPopulations();
            //DashboardPlot3.Plot.YAxis.TickLabelNotation();
        }
    }
}
