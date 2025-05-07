using ProteoformExplorer.Core;
using ProteoformExplorer.GuiFunctions;
using ScottPlot.Drawing;
using ScottPlot.Statistics;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ProteoformExplorer.Wpf.Pages;
using UsefulProteomicsDatabases;

namespace ProteoformExplorer.Wpf
{
    /// <summary>
    /// Interaction logic for Dashboard.xaml
    /// </summary>
    public partial class Dashboard : Page
    {
        public static Page1_QuantifiedTic Page1;
        public static Page2_SpeciesView Page2;
        public static Page3_StackedIons Page3;
        public static ProteoformFamilyVisualization ProteoformFamilyVisualization;
        public SettingsViewModel SettingsViewModel;
        private static ML_Trainer ML_Trainer;
        public static bool TicLollipopPlot = true;
        public static bool EnvelopeCountPlot = true;
        public static bool MassHistogramPlot = true;
        public static bool TicDashboardItem = true;
        public static bool SpeciesViewDashboardItem = true;
        public static bool WaterfallPlotDashboardItem = true;
        public static bool ProteoformVisualization = false;

        public Dashboard()
        {
            InitializeComponent();

            InitializeDashboard();
            SettingsViewModel = new();
        }

        private void InitializeDashboard()
        {
            var allDashboardElements = new List<UIElement> { DashboardPlot1, DashboardPlot2, DashboardPlot3,
                ticButton, speciesViewButton, waterfallButton, proteoformFamilyVisualization };

            foreach (var item in allDashboardElements)
            {
                item.Visibility = Visibility.Hidden;
            }

            // the dashboard is hardcoded at 2 rows for now
            UIElement[][] dashboardElements = new UIElement[2][];

            List<UIElement> dashboardRow1 = new List<UIElement>();
            List<UIElement> dashboardRow2 = new List<UIElement>();

            if (TicLollipopPlot)
            {
                dashboardRow1.Add(DashboardPlot1);
            }
            if (EnvelopeCountPlot)
            {
                dashboardRow1.Add(DashboardPlot2);
            }
            if (MassHistogramPlot)
            {
                dashboardRow1.Add(DashboardPlot3);
            }
            if (TicDashboardItem)
            {
                dashboardRow2.Add(ticButton);
            }
            if (SpeciesViewDashboardItem)
            {
                dashboardRow2.Add(speciesViewButton);
            }
            if (WaterfallPlotDashboardItem)
            {
                dashboardRow2.Add(waterfallButton);
            }
            if (ProteoformVisualization)
            {
                dashboardRow2.Add(proteoformFamilyVisualization);
            }

            dashboardElements[0] = dashboardRow1.ToArray();
            dashboardElements[1] = dashboardRow2.ToArray();

            double maxColumns = dashboardElements.Max(p => p.Length);

            // arrange the dashboard elements in the grid
            for (int i = 0; i < dashboardElements.Length; i++)
            {
                var row = new Grid();
                row.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent);

                var rowDefinition = new RowDefinition();
                rowDefinition.Height = new GridLength(1.0, GridUnitType.Star);
                dashboardElementsGrid.RowDefinitions.Add(rowDefinition);

                dashboardElementsGrid.Children.Add(row);
                Grid.SetRow(row, i);

                var dashboardElementsInRow = dashboardElements[i];

                // add padding before dashboard elements
                var paddingSize = (maxColumns - dashboardElementsInRow.Length) / 4;
                var paddingColumnDefinition = new ColumnDefinition();
                paddingColumnDefinition.Width = new GridLength(paddingSize, GridUnitType.Star);
                row.ColumnDefinitions.Add(paddingColumnDefinition);

                // add dashboard elements
                for (int j = 0; j < dashboardElementsInRow.Length; j++)
                {
                    double width = dashboardElementsInRow.Length / maxColumns;

                    var columnDefinition = new ColumnDefinition();
                    columnDefinition.Width = new GridLength(width, GridUnitType.Star);
                    row.ColumnDefinitions.Add(columnDefinition);

                    var dashboardElement = dashboardElementsInRow[j];

                    theGrid.Children.Remove(dashboardElement);
                    row.Children.Add(dashboardElement);
                    Grid.SetColumn(dashboardElement, j + 1);
                    dashboardElement.Visibility = Visibility.Visible;
                }

                // add padding after dashboard elements
                paddingColumnDefinition = new ColumnDefinition();
                paddingColumnDefinition.Width = new GridLength(paddingSize, GridUnitType.Star);
                row.ColumnDefinitions.Add(paddingColumnDefinition);
            }

            if (Page1 == null)
            {
                Page1 = new Page1_QuantifiedTic();
                Page2 = new Page2_SpeciesView();
                Page3 = new Page3_StackedIons();
                ProteoformFamilyVisualization = new ProteoformFamilyVisualization();
                //ML_Trainer = new ML_Trainer();
            }

            WpfFunctions.CalculateDpiSettings(DashboardPlot1);

            PlottingFunctions.DrawPercentTicPerFileInfoDashboardPlot(DashboardPlot1.Plot, out var errors);

            if (errors.Any())
            {
                MessageBox.Show("An error occurred creating the percent TIC dashboard chart: " + errors.First());
            }

            PlottingFunctions.DrawNumEnvelopesDashboardPlot(DashboardPlot2.Plot, out errors);

            if (errors.Any())
            {
                MessageBox.Show("An error occurred creating the num envelopes dashboard chart: " + errors.First());
            }

            PlottingFunctions.DrawMassDistributionsDashboardPlot(DashboardPlot3.Plot, out errors);

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

        private void ProteoformFamilyVisualization_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(ProteoformFamilyVisualization);
        }

        private void MLTrainer_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(ML_Trainer);
        }

        private void goToDataLoading_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new DataLoading());
        }

        private void SettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(SettingsViewModel);
            settingsWindow.ShowDialog();
        }
    }
}
