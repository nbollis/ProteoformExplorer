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
        }

        private void InitializeDashboard()
        {
            if (Page1 == null)
            {
                Page1 = new Page1_QuantifiedTic();
                Page2 = new Page2_SpeciesView();
                Page3 = new Page3_StackedIons();
            }

            GuiFunctions.DrawPercentTicInfo(DashboardPlot1, out var errors);

            if (errors.Any())
            {
                MessageBox.Show("An error occurred creating the percent TIC dashboard chart: " + errors.First());
            }

            GuiFunctions.DrawNumEnvelopes(DashboardPlot2, out errors);

            if (errors.Any())
            {
                MessageBox.Show("An error occurred creating the num envelopes dashboard chart: " + errors.First());
            }

            GuiFunctions.DrawMassDistributions(DashboardPlot3, out errors);

            if (errors.Any())
            {
                MessageBox.Show("An error occurred creating the mass distribution dashboard chart: " + errors.First());
            }
        }

        private void Chart1_Click(object sender, RoutedEventArgs e)
        {
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

        private void goToDataLoading_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new DataLoading());
        }
    }
}
