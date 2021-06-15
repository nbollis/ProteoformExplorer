using Deconvoluter;
using IO.MzML;
using IO.ThermoRawFileReader;
using MassSpectrometry;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProteoformExplorer
{
    /// <summary>
    /// Interaction logic for HomePage.xaml
    /// </summary>
    public partial class HomePage : Page
    {
        private static ObservableCollection<string> SelectedFilePaths;
        private static ObservableCollection<string> LoadedSpectraFilePaths;
        private static Dictionary<string, DynamicDataConnection> SpectraFiles;
        private static ObservableCollection<AnnotatedSpecies> LoadedAnnotatedSpecies;
        private static Page1_QuantifiedTic Page1;
        private static Page2_SpeciesView Page2;
        public static DeconvolutionEngine DeconvolutionEngine;

        public HomePage()
        {
            InitializeComponent();

            if (Page1 == null)
            {
                DeconvolutionEngine = new DeconvolutionEngine(2000, 0.3, 4, 0.3, 3, 5, 2, 60, 2);
                LoadedAnnotatedSpecies = new ObservableCollection<AnnotatedSpecies>();
                SpectraFiles = new Dictionary<string, DynamicDataConnection>();
                SelectedFilePaths = new ObservableCollection<string>();
                LoadedSpectraFilePaths = new ObservableCollection<string>();

                Page1 = new Page1_QuantifiedTic(SpectraFiles, LoadedAnnotatedSpecies, SelectedFilePaths, LoadedSpectraFilePaths);
                Page2 = new Page2_SpeciesView(SpectraFiles, LoadedAnnotatedSpecies, SelectedFilePaths, LoadedSpectraFilePaths);
            }
        }

        public static void LoadDataButton_Click(object sender, RoutedEventArgs e)
        {
            LoadedSpectraFilePaths.Clear();

            foreach (var item in SpectraFiles)
            {
                item.Value.CloseDynamicConnection();
            }

            SpectraFiles.Clear();

            foreach (var file in SelectedFilePaths)
            {
                LoadFile(file);
            }

            //TODO: this displays when an error is not found, so if you X out of the window it displays by mistake
            MessageBox.Show("Files successfully loaded");
        }

        public static void SelectDataButton_Click(object sender, RoutedEventArgs e)
        {
            string filterString = string.Join(";", InputReaderParser.AcceptedFileFormats.Select(p => "*" + p));

            Microsoft.Win32.OpenFileDialog openFileDialog1 = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Files(" + filterString + ")|" + filterString,
                FilterIndex = 1,
                RestoreDirectory = true,
                Multiselect = true
            };

            if (openFileDialog1.ShowDialog() == true)
            {
                SelectedFilePaths.Clear();

                foreach (var filePath in openFileDialog1.FileNames.OrderBy(p => p))
                {
                    SelectedFilePaths.Add(filePath);
                }

                Page1.RefreshPage();
            }
        }

        private static void LoadFile(string path)
        {
            var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();

            if (ext == ".raw")
            {
                SpectraFiles.Add(path, new ThermoDynamicData(path));
                LoadedSpectraFilePaths.Add(path);
            }
            else if (ext == ".mzml")
            {
                SpectraFiles.Add(path, new MzmlDynamicData(path));
                LoadedSpectraFilePaths.Add(path);
            }
            else if (ext == ".psmtsv" || ext == ".tsv" || ext == ".txt")
            {
                var items = InputReaderParser.ReadSpeciesFromFile(path, out var errors);

                foreach (var item in items)
                {
                    LoadedAnnotatedSpecies.Add(item);
                }
            }
            else
            {
                MessageBox.Show("Unrecognized file format: " + ext);
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

        }

        private void Chart4_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Chart5_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Chart6_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
