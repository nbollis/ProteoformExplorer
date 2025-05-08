using IO.MzML;
using IO.ThermoRawFileReader;
using MassSpectrometry;
using Nett;
using ProteoformExplorer.Core;
using ProteoformExplorer.GuiFunctions;
using Readers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using UsefulProteomicsDatabases;

namespace ProteoformExplorer.Wpf
{
    /// <summary>
    /// Interaction logic for DataLoading.xaml
    /// </summary>
    public partial class DataLoading : Page
    {
        public static ObservableCollection<FileForDataGrid> FilesToLoad;
        public static ObservableCollection<FileForDataGrid> LoadedSpectraFiles;
        private static BackgroundWorker worker;

        public DataLoading()
        {
            InitializeComponent();

            if (DataManagement.AllLoadedAnnotatedSpecies == null)
            {
                DataManagement.AllLoadedAnnotatedSpecies = new ObservableCollection<AnnotatedSpecies>();
                DataManagement.SpectraFiles = new Dictionary<string, CachedSpectraFileData>();
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
            try
            {
                LoadedSpectraFiles.Clear();

                foreach (var item in DataManagement.SpectraFiles)
                {
                    item.Value.DataFile.Value.CloseDynamicConnection();
                }

                DataManagement.SpectraFiles.Clear();

                // load files in the background
                worker.RunWorkerAsync();
            }
            catch (Exception exep)
            {
                MessageBox.Show("An error occurred while loading files: " + exep.Message);
            }
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

                    if (DataManagement.SpectraFiles.Any())
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

        public void GoToDashboard_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Dashboard());
        }

        public void AddFileToLoad(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();

            if (InputReaderParser.AcceptedSpectraFileFormats.Contains(ext) || InputReaderParser.AcceptedTextFileFormats.Contains(ext))
            {
                var file = new FileForDataGrid(filePath, out var errors);

                if (errors.Any())
                {
                    MessageBox.Show(errors.First());
                    return;
                }

                FilesToLoad.Add(file);
            }
        }

        private static void LoadFile(FileForDataGrid file)
        {
            var ext = Path.GetExtension(file.FullFilePath).ToLowerInvariant();
            var fileName = Path.GetFileName(file.FullFilePath);

            if (ext == ".raw" || ext == ".mzml") 
            { 
                var kvp = new KeyValuePair<string, MsDataFile>(fileName, MsDataFileReader.GetDataFile(file.FullFilePath));

                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    LoadedSpectraFiles.Add(file);
                });

                DataManagement.SpectraFiles.Add(fileName, new CachedSpectraFileData(kvp));
            }
            else if (ext == ".psmtsv" || ext == ".tsv" || ext == ".txt" || ext == ".feature")
            {
                var items = InputReaderParser.ReadSpeciesFromFile(file.FullFilePath, out var errors);

                lock(DataManagement.AllLoadedAnnotatedSpecies)
                {
                    foreach (var item in items)
                    {
                        DataManagement.AllLoadedAnnotatedSpecies.Add(item);
                    }
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
                    AddFileToLoad(filePath);
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
            int itemsLoaded = 0;
            double itemsToLoad = FilesToLoad.Count +
                FilesToLoad.Count(p => Path.GetExtension(p.FileNameWithExtension).ToLowerInvariant() == ".raw" || Path.GetExtension(p.FileNameWithExtension).ToLowerInvariant() == ".mzml");

            BackgroundWorker worker = (BackgroundWorker)sender;
            worker.ReportProgress(0);

            // this calculates all the stuff needed for deconvolution, like averagine distributions
            var temp = PfmXplorerUtil.DeconvolutionEngine;

            // load the selected files
            Parallel.ForEach(FilesToLoad, file =>
            {
                LoadFile(file);
                lock (worker)
                {
                    Interlocked.Increment(ref itemsLoaded);
                    int progress = (int)(itemsLoaded / itemsToLoad * 100);
                    worker.ReportProgress(progress);
                }
            });

            // pre-compute TIC stuff and report loading progress
            foreach (var file in DataManagement.SpectraFiles)
            {
                file.Value.CreateAnnotatedDeconvolutionFeatures(DataManagement.AllLoadedAnnotatedSpecies.ToList());
                file.Value.GetTicChromatogram(GuiSettings.TicRollingAverage);
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
