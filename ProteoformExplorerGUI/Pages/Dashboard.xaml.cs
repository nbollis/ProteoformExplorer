using ProteoformExplorer.Objects;
using ScottPlot.Drawing;
using ScottPlot.Statistics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using UsefulProteomicsDatabases;

namespace ProteoformExplorer.ProteoformExplorerGUI
{
    /// <summary>
    /// Interaction logic for Dashboard.xaml
    /// </summary>
    public partial class Dashboard : Page
    {
        private static Page1_QuantifiedTic Page1;
        private static Page2_SpeciesView Page2;
        private static Page3_StackedIons Page3;
        public static bool TicLollipopPlot = true;
        public static bool EnvelopeCountPlot = true;
        public static bool MassHistogramPlot = true;
        public static bool TicDashboardItem = true;
        public static bool SpeciesViewDashboardItem = true;
        public static bool WaterfallPlotDashboardItem = true;
        public static bool ProteoformVisualization = true;

        public Dashboard()
        {
            InitializeComponent();
            Loaders.LoadElements();
            
            new DataLoading();

            InitializeDashboard();

            if (!TicLollipopPlot)
            {
                DashboardPlot1.Visibility = Visibility.Hidden;
            }

            if (!EnvelopeCountPlot)
            {
                DashboardPlot2.Visibility = Visibility.Hidden;
            }

            if (!MassHistogramPlot)
            {
                DashboardPlot3.Visibility = Visibility.Hidden;
            }

            if (!TicDashboardItem)
            {
                ticButton.Visibility = Visibility.Hidden;
            }

            if (!SpeciesViewDashboardItem)
            {
                speciesViewButton.Visibility = Visibility.Hidden;
            }

            if (!WaterfallPlotDashboardItem)
            {
                waterfallButton.Visibility = Visibility.Hidden;
            }

            if (!ProteoformVisualization)
            {
                //TODO
                //DashboardPlot1.Visibility = Visibility.Hidden;
            }
        }

        private void InitializeDashboard()
        {
            if (Page1 == null)
            {
                Page1 = new Page1_QuantifiedTic();
                Page2 = new Page2_SpeciesView();
                Page3 = new Page3_StackedIons();
            }

            GuiFunctions.DrawPercentTicPerFileInfoDashboardPlot(DashboardPlot1, out var errors);

            if (errors.Any())
            {
                MessageBox.Show("An error occurred creating the percent TIC dashboard chart: " + errors.First());
            }

            GuiFunctions.DrawNumEnvelopesDashboardPlot(DashboardPlot2, out errors);

            if (errors.Any())
            {
                MessageBox.Show("An error occurred creating the num envelopes dashboard chart: " + errors.First());
            }

            GuiFunctions.DrawMassDistributionsDashboardPlot(DashboardPlot3, out errors);

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
