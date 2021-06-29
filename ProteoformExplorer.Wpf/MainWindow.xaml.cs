using System.Windows;

namespace ProteoformExplorer.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //Dashboard.ProteoformVisualization = true;
            _NavigationFrame.Navigate(new DataLoading());
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
