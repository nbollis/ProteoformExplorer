using IO.MzML;
using IO.ThermoRawFileReader;
using MassSpectrometry;
using ProteoformExplorer;
using ProteoformExplorerObjects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using UsefulProteomicsDatabases;

namespace GUI.Modules
{
    /// <summary>
    /// Interaction logic for DataLoading.xaml
    /// </summary>
    public partial class DataLoading : Page
    {
        public static ObservableCollection<string> SelectedFilePaths;
        public static ObservableCollection<string> LoadedSpectraFileNamesWithExtensions;
        public static Dictionary<string, CachedSpectraFileData> SpectraFiles;
        public static ObservableCollection<AnnotatedSpecies> AllLoadedAnnotatedSpecies;
        public static KeyValuePair<string, CachedSpectraFileData> CurrentlySelectedFile;
        private static BackgroundWorker worker;

        public DataLoading()
        {
            InitializeComponent();

            AllLoadedAnnotatedSpecies = new ObservableCollection<AnnotatedSpecies>();
            SpectraFiles = new Dictionary<string, CachedSpectraFileData>();
            SelectedFilePaths = new ObservableCollection<string>();
            LoadedSpectraFileNamesWithExtensions = new ObservableCollection<string>();

            selectedFiles.ItemsSource = SelectedFilePaths;

            loadFiles.Click += new RoutedEventHandler(LoadDataButton_Click);
            goToDashboard.Click += new RoutedEventHandler(GoToDashboard_Click);

            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(LoadFilesInBackground);
            worker.ProgressChanged += new ProgressChangedEventHandler(WorkerProgressChanged);
            worker.WorkerReportsProgress = true;

            RefreshPage();
        }

        public static void LoadDataButton_Click(object sender, RoutedEventArgs e)
        {
            LoadedSpectraFileNamesWithExtensions.Clear();

            foreach (var item in SpectraFiles)
            {
                item.Value.DataFile.Value.CloseDynamicConnection();
            }

            SpectraFiles.Clear();

            // load files in the background
            worker.RunWorkerAsync();
        }

        public void RefreshPage()
        {
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                dataLoadingProgressBar.Visibility = Visibility.Hidden;

                if (SelectedFilePaths.Any())
                {
                    dragAndDropFileLoadingArea.Visibility = Visibility.Hidden;
                    selectedFiles.Visibility = Visibility.Visible;

                    if (SpectraFiles.Any())
                    {
                        goToDashboard.Visibility = Visibility.Visible;
                        loadFiles.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        goToDashboard.Visibility = Visibility.Hidden;
                        loadFiles.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    dragAndDropFileLoadingArea.Visibility = Visibility.Visible;
                    selectedFiles.Visibility = Visibility.Hidden;
                }
            });
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
            }
        }

        public void GoToDashboard_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Dashboard());
        }

        private static void LoadFile(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            var fileName = Path.GetFileName(path);

            if (ext == ".raw")
            {
                var kvp = new KeyValuePair<string, DynamicDataConnection>(fileName, new ThermoDynamicData(path));

                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    LoadedSpectraFileNamesWithExtensions.Add(fileName);
                });

                SpectraFiles.Add(fileName, new CachedSpectraFileData(kvp));
            }
            else if (ext == ".mzml")
            {
                var kvp = new KeyValuePair<string, DynamicDataConnection>(fileName, new MzmlDynamicData(path));

                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    LoadedSpectraFileNamesWithExtensions.Add(fileName);
                });

                SpectraFiles.Add(fileName, new CachedSpectraFileData(kvp));
            }
            else if (ext == ".psmtsv" || ext == ".tsv" || ext == ".txt")
            {
                var items = InputReaderParser.ReadSpeciesFromFile(path, out var errors);

                foreach (var item in items)
                {
                    AllLoadedAnnotatedSpecies.Add(item);
                }
            }
            else
            {
                MessageBox.Show("Unrecognized file format: " + ext);
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files != null)
            {
                foreach (var filePath in files.OrderBy(p => p))
                {
                    var ext = Path.GetExtension(filePath).ToLowerInvariant();

                    if (InputReaderParser.AcceptedFileFormats.Contains(ext))
                    {
                        SelectedFilePaths.Add(filePath);
                    }
                }
            }

            RefreshPage();
        }

        private void LoadFilesInBackground(object sender, DoWorkEventArgs e)
        {
            RefreshPage();

            // double-count spectra files for transparency because we're going to load some extra stuff from them
            double itemsLoaded = 0;
            double itemsToLoad = SelectedFilePaths.Count + 
                SelectedFilePaths.Count(p => Path.GetExtension(p).ToLowerInvariant() == ".raw" || Path.GetExtension(p).ToLowerInvariant() == ".mzml");

            BackgroundWorker worker = (BackgroundWorker)sender;
            worker.ReportProgress(0);

            // this calculates all the stuff needed for deconvolution, like averagine distributions
            Loaders.LoadElements();
            Dashboard.DeconvolutionEngine = new Deconvoluter.DeconvolutionEngine(2000, 0.3, 4, 0.3, 3, 5, 2, 60, 2);

            // load the selected files
            foreach (var file in SelectedFilePaths)
            {
                LoadFile(file);
                itemsLoaded++;

                int progress = (int)(itemsLoaded / itemsToLoad * 100);
                worker.ReportProgress(progress);
            }

            // pre-compute TIC stuff and report loading progress
            foreach (var file in SpectraFiles)
            {
                file.Value.BuildScanToSpeciesDictionary(AllLoadedAnnotatedSpecies.ToList());
                file.Value.GetTicChromatogram();
                itemsLoaded++;

                int progress = (int)(itemsLoaded / itemsToLoad * 100);
                worker.ReportProgress(progress);
            }

            RefreshPage();
            MessageBox.Show("Files successfully loaded");
        }

        private void WorkerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                dataLoadingProgressBar.Visibility = Visibility.Visible;
            });

            dataLoadingProgressBar.Maximum = 100;
            dataLoadingProgressBar.Value = e.ProgressPercentage;
        }
    }
}
