using System.Windows;

namespace ProteoformExplorer.ProteoformExplorerGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //_NavigationFrame.Navigate(new ProteoformFamilyVisualization());
            _NavigationFrame.Navigate(new DataLoading());
            //_NavigationFrame.Navigate(new ML_Trainer());
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
