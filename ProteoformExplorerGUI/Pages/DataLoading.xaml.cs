using IO.MzML;
using IO.ThermoRawFileReader;
using MassSpectrometry;
using ProteoformExplorer.Objects;
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

namespace ProteoformExplorer.ProteoformExplorerGUI
{
    /// <summary>
    /// Interaction logic for DataLoading.xaml
    /// </summary>
    public partial class DataLoading : Page
    {
        public static ObservableCollection<FileForDataGrid> FilesToLoad;
        public static ObservableCollection<FileForDataGrid> LoadedSpectraFiles;
        public static Dictionary<string, CachedSpectraFileData> SpectraFiles;
        public static ObservableCollection<AnnotatedSpecies> AllLoadedAnnotatedSpecies;
        public static KeyValuePair<string, CachedSpectraFileData> CurrentlySelectedFile;
        private static BackgroundWorker worker;

        public DataLoading()
        {
            InitializeComponent();

            Loaders.LoadElements();

            if (AllLoadedAnnotatedSpecies == null)
            {
                AllLoadedAnnotatedSpecies = new ObservableCollection<AnnotatedSpecies>();
                SpectraFiles = new Dictionary<string, CachedSpectraFileData>();
                FilesToLoad = new ObservableCollection<FileForDataGrid>();
                LoadedSpectraFiles = new ObservableCollection<FileForDataGrid>();
            }

            filesToLoad.ItemsSource = FilesToLoad;

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
            LoadedSpectraFiles.Clear();

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

                if (FilesToLoad.Any())
                {

                    dragAndDropFileLoadingArea.Visibility = Visibility.Hidden;
                    filesToLoad.Visibility = Visibility.Visible;

                    if (SpectraFiles.Any())
                    {
                        // files have been loaded
                        goToDashboard.Visibility = Visibility.Visible;
                        loadFiles.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        // files have been dragged in but not loaded yet
                        goToDashboard.Visibility = Visibility.Hidden;
                        loadFiles.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    // no files have been dragged in yet
                    dragAndDropFileLoadingArea.Visibility = Visibility.Visible;
                    filesToLoad.Visibility = Visibility.Hidden;
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
                FilesToLoad.Clear();

                foreach (var filePath in openFileDialog1.FileNames.OrderBy(p => p))
                {
                    FilesToLoad.Add(new FileForDataGrid(filePath));
                }
            }
        }

        public void GoToDashboard_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Dashboard());
        }

        private static void LoadFile(FileForDataGrid file)
        {
            var ext = Path.GetExtension(file.FullFilePath).ToLowerInvariant();
            var fileName = Path.GetFileName(file.FullFilePath);

            if (ext == ".raw")
            {
                var kvp = new KeyValuePair<string, DynamicDataConnection>(fileName, new ThermoDynamicData(file.FullFilePath));

                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    LoadedSpectraFiles.Add(file);
                });

                SpectraFiles.Add(fileName, new CachedSpectraFileData(kvp));
            }
            else if (ext == ".mzml")
            {
                var kvp = new KeyValuePair<string, DynamicDataConnection>(fileName, new MzmlDynamicData(file.FullFilePath));

                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    LoadedSpectraFiles.Add(file);
                });

                SpectraFiles.Add(fileName, new CachedSpectraFileData(kvp));
            }
            else if (ext == ".psmtsv" || ext == ".tsv" || ext == ".txt")
            {
                var items = InputReaderParser.ReadSpeciesFromFile(file.FullFilePath, out var errors);

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
                        FilesToLoad.Add(new FileForDataGrid(filePath));
                    }
                }
            }

            RefreshPage();
        }

        private void LoadFilesInBackground(object sender, DoWorkEventArgs e)
        {
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                loadFiles.Visibility = Visibility.Hidden;
                goToDashboard.Visibility = Visibility.Hidden;
            });

            // double-count spectra files for transparency because we're going to load some extra stuff from them
            double itemsLoaded = 0;
            double itemsToLoad = FilesToLoad.Count +
                FilesToLoad.Count(p => Path.GetExtension(p.FileNameWithExtension).ToLowerInvariant() == ".raw" || Path.GetExtension(p.FileNameWithExtension).ToLowerInvariant() == ".mzml");

            BackgroundWorker worker = (BackgroundWorker)sender;
            worker.ReportProgress(0);

            // this calculates all the stuff needed for deconvolution, like averagine distributions
            var temp = PfmXplorerUtil.DeconvolutionEngine;

            // load the selected files
            foreach (var file in FilesToLoad)
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
