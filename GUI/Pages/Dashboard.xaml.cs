using Deconvoluter;
using GUI.Modules;
using IO.MzML;
using IO.ThermoRawFileReader;
using MassSpectrometry;
using ProteoformExplorerObjects;
using Proteomics.ProteolyticDigestion;
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

        }
    }
}
