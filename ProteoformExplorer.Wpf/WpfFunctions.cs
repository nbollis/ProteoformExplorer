using ProteoformExplorer.Core;
using ProteoformExplorer.GuiFunctions;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProteoformExplorer.Wpf
{
    public static class WpfFunctions
    {
        public static void CalculateDpiSettings(WpfPlot plot)
        {
            // DPI scaling, for high-resolution monitors
            if (GuiSettings.DpiScaling)
            {
                PresentationSource source = PresentationSource.FromVisual(plot);

                double dpiX = 0;
                double dpiY = 0;
                if (source != null)
                {
                    dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
                    dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;
                }
                else
                {
                    using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                    {
                        dpiX = g.DpiX;
                        dpiY = g.DpiY;
                    }
                }

                if (dpiX < 96 || dpiY < 96)
                {
                    return;
                }

                double xScale = dpiX / 96;
                double yScale = dpiY / 96;

                GuiSettings.DpiScalingX = xScale;
                GuiSettings.DpiScalingY = yScale;
            }
        }


        public static void PopulateTreeViewWithSpeciesAndCharges(ObservableCollection<INode> SelectableAnnotatedSpecies)
        {
            SelectableAnnotatedSpecies.Clear();
            var nameWithoutExtension = PfmXplorerUtil.GetFileNameWithoutExtension(DataManagement.CurrentlySelectedFile.Key);

            foreach (AnnotatedSpecies species in DataManagement.AllLoadedAnnotatedSpecies.Where(p => p.SpectraFileNameWithoutExtension == nameWithoutExtension))
            {
                var parentNode = new AnnotatedSpeciesNode(species);
                SelectableAnnotatedSpecies.Add(parentNode);

                if (species.DeconvolutionFeature != null)
                {
                    foreach (var charge in species.DeconvolutionFeature.Charges.Where(p => species.Identification == null || species.Identification.PrecursorChargeState != p))
                    {
                        var childNode = new AnnotatedSpeciesNodeSpecificCharge(species, charge, "z=" + charge.ToString());
                        parentNode.Charges.Add(childNode);
                    }
                }
                if (species.Identification != null)
                {
                    int charge = species.Identification.PrecursorChargeState;
                    var childNode = new AnnotatedSpeciesNodeSpecificCharge(species, charge, "z=" + charge.ToString() + " (ID)");
                    parentNode.Charges.Add(childNode);
                }
            }
        }

        public static void ShowOrHideSpectraFileList(ListView DataListView, GridSplitter gridSplitter)
        {
            if (DataListView.Visibility == Visibility.Hidden)
            {
                DataListView.Visibility = Visibility.Visible;
                gridSplitter.Visibility = Visibility.Visible;
            }
            else
            {
                DataListView.Visibility = Visibility.Hidden;
                gridSplitter.Visibility = Visibility.Hidden;
            }
        }


        public static void OnSpectraFileChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItems = ((ListView)sender).SelectedItems;

            if (selectedItems != null && selectedItems.Count >= 1)
            {
                var spectraFileName = ((FileForDataGrid)selectedItems[0]).FileNameWithExtension;

                if (DataManagement.SpectraFiles.ContainsKey(spectraFileName))
                {
                    DataManagement.CurrentlySelectedFile = DataManagement.SpectraFiles.First(p => p.Key == spectraFileName);
                }
                else
                {
                    MessageBox.Show("The spectra file " + spectraFileName + " has not been loaded yet");
                    return;
                }
            }
        }

        public static double GetXPositionFromMouseClickOnChart(object sender, MouseButtonEventArgs e)
        {
            var plot = (WpfPlot)sender;

            if (plot == null)
            {
                return double.NaN;
            }

            int pixelX = (int)e.MouseDevice.GetPosition(plot).X;
            int pixelY = (int)e.MouseDevice.GetPosition(plot).Y;

            (double coordinateX, double coordinateY) = plot.GetMouseCoordinates();

            return coordinateX;
        }
    }
}
