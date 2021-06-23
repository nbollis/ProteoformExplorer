using GUI.Modules;
using ProteoformExplorer;
using ScottPlot.Plottable;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace GUI.Pages
{
    public enum VisualizedType { Gene, Transcript, TheoreticalProteoform, TopDownExperimentalProteoform, IntactMassExperimentalProteoform, QuantifiedExperimentalProteoform }

    /// <summary>
    /// Interaction logic for ProteoformFamilyVisualization.xaml
    /// </summary>
    public partial class ProteoformFamilyVisualization : Page
    {
        public ProteoformFamilyVisualization()
        {
            InitializeComponent();

            var exampleFamily = new List<VisualizedProteoformFamilyMember>
            {
                new VisualizedProteoformFamilyMember(VisualizedType.Gene, @"TestGene", 10, 0),
                new VisualizedProteoformFamilyMember(VisualizedType.TheoreticalProteoform, @"Unmodified", 15, 5),
                new VisualizedProteoformFamilyMember(VisualizedType.TopDownExperimentalProteoform, @"Unmodified", 15, 10),
                new VisualizedProteoformFamilyMember(VisualizedType.IntactMassExperimentalProteoform, @"Acetyl", 5, 5),
                new VisualizedProteoformFamilyMember(VisualizedType.QuantifiedExperimentalProteoform, @"Unmodified", 7, 7, new List<double> { 1.0, 2.0 }, isStatisticallySignificant: true),
            };

            exampleFamily[0].Node.AddConnection(exampleFamily[1].Node, "0 Da");
            exampleFamily[0].Node.AddConnection(exampleFamily[2].Node, "0 Da");
            exampleFamily[1].Node.AddConnection(exampleFamily[4].Node, "0 Da");
            exampleFamily[1].Node.AddConnection(exampleFamily[3].Node, "42 Da");

            DrawProteoformFamily(exampleFamily);

            // unsubscribe from the default right-click menu event
            pfmFamilyVisualizationChart.RightClicked -= pfmFamilyVisualizationChart.DefaultRightClickEvent;

            // add your own custom event
            pfmFamilyVisualizationChart.RightClicked += DeployCustomMenu;
        }

        public void DrawProteoformFamily(List<VisualizedProteoformFamilyMember> proteoformFamily)
        {
            pfmFamilyVisualizationChart.Plot.AxisScaleLock(true);
            pfmFamilyVisualizationChart.Plot.Grid(false);
            pfmFamilyVisualizationChart.Plot.Frameless();

            foreach (var proteoform in proteoformFamily)
            {
                foreach (var connection in proteoform.Node.Edges)
                {
                    connection.VisualRepresentation.LineWidth = 1;
                    connection.VisualRepresentation.Color = Color.FromArgb(124, 124, 124); // gray
                    connection.TextAnnotation.Color = Color.FromArgb(255, 113, 79); // orange text annotation for edge

                    pfmFamilyVisualizationChart.Plot.Add(connection.VisualRepresentation);
                    pfmFamilyVisualizationChart.Plot.Add(connection.TextAnnotation);
                }
            }

            foreach (var proteoform in proteoformFamily)
            {
                foreach (var item in proteoform.Node.VisualRepresentation)
                {
                    pfmFamilyVisualizationChart.Plot.Add(item);
                }
                pfmFamilyVisualizationChart.Plot.Add(proteoform.Node.TextAnnotation);
            }
        }

        private void backToDashboardButton_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Dashboard());
        }

        private void DeployCustomMenu(object sender, EventArgs e)
        {
            //var test = new List<string> { "test1", "test2", "test3" };
            //var submenu = new ContextMenu();
            ////foreach (var file in DataLoading.SpectraFiles)
            ////{
            ////    var item = new MenuItem() { Header = file.Key };
            ////    submenu.Items.Add(item);
            ////}
            //foreach (var file in test)
            //{
            //    var item = new MenuItem() { Header = file };
            //    submenu.Items.Add(item);
            //}

            //MenuItem addSinMenuItem = new MenuItem() { Header = "Plot in file...", sub = submenu };
            ////addSinMenuItem.Click += AddSine;

            //ContextMenu rightClickMenu = new ContextMenu();
            //rightClickMenu.Items.Add(addSinMenuItem);

            //rightClickMenu.IsOpen = true;
        }
    }

    public class VisualizedProteoformFamilyMember
    {
        public VisualizedType Type { get; private set; }
        public Node Node { get; private set; }
        public List<double> QuantifiedIntensities { get; private set; }
        public bool IsStatisticallySignificantlyDifferent { get; private set; }

        public VisualizedProteoformFamilyMember(VisualizedType type, string nodeAnnotation, double x, double y, List<double> quantifiedIntensities = null,
            bool isStatisticallySignificant = false)
        {
            Type = type;
            Node = new Node(x, y, nodeAnnotation);
            QuantifiedIntensities = quantifiedIntensities;
            IsStatisticallySignificantlyDifferent = isStatisticallySignificant;
            BuildProteoformVisualRepresentation();
        }

        private void BuildProteoformVisualRepresentation()
        {
            Node.TextAnnotation.Color = Color.Black; // black text annotation for node

            switch (Type)
            {
                case VisualizedType.Gene:
                    var qualitativeMarker = new ScatterPlot(new double[] { Node.X }, new double[] { Node.Y });
                    qualitativeMarker.MarkerSize = 70;
                    qualitativeMarker.MarkerShape = ScottPlot.MarkerShape.filledSquare;
                    qualitativeMarker.Color = Color.FromArgb(255, 116, 175); // pink
                    Node.VisualRepresentation = new List<IPlottable> { qualitativeMarker };
                    break;

                case VisualizedType.Transcript:
                    qualitativeMarker = new ScatterPlot(new double[] { Node.X }, new double[] { Node.Y });
                    qualitativeMarker.MarkerSize = 70;
                    qualitativeMarker.MarkerShape = ScottPlot.MarkerShape.filledDiamond;
                    qualitativeMarker.Color = Color.FromArgb(255, 116, 175); // pink
                    Node.VisualRepresentation = new List<IPlottable> { qualitativeMarker };
                    break;

                case VisualizedType.TheoreticalProteoform:
                    qualitativeMarker = new ScatterPlot(new double[] { Node.X }, new double[] { Node.Y });
                    qualitativeMarker.MarkerSize = 70;
                    qualitativeMarker.MarkerShape = ScottPlot.MarkerShape.filledCircle;
                    qualitativeMarker.Color = Color.FromArgb(0, 183, 62); // green
                    Node.VisualRepresentation = new List<IPlottable> { qualitativeMarker };
                    break;

                case VisualizedType.TopDownExperimentalProteoform:
                    qualitativeMarker = new ScatterPlot(new double[] { Node.X }, new double[] { Node.Y });
                    qualitativeMarker.MarkerSize = 70;
                    qualitativeMarker.MarkerShape = ScottPlot.MarkerShape.filledCircle;
                    qualitativeMarker.Color = Color.FromArgb(144, 123, 189); // purple
                    Node.VisualRepresentation = new List<IPlottable> { qualitativeMarker };
                    break;

                case VisualizedType.IntactMassExperimentalProteoform:
                    qualitativeMarker = new ScatterPlot(new double[] { Node.X }, new double[] { Node.Y });
                    qualitativeMarker.MarkerSize = 70;
                    qualitativeMarker.MarkerShape = ScottPlot.MarkerShape.filledCircle;
                    qualitativeMarker.Color = Color.FromArgb(0, 193, 245); // blue
                    Node.VisualRepresentation = new List<IPlottable> { qualitativeMarker };
                    break;

                case VisualizedType.QuantifiedExperimentalProteoform:
                    List<IPlottable> polygons = new List<IPlottable>();
                    (double x, double y) centerOfCircle = (Node.X, Node.Y);
                    double volume = QuantifiedIntensities.Sum();
                    double radius = Math.Sqrt(volume / Math.PI);
                    double radiansStep = 0.01;

                    double radianStart = Math.PI / 2.0;
                    foreach (var intensity in QuantifiedIntensities)
                    {
                        List<double> thetas = new List<double>();
                        double endRadians = radianStart + (intensity / volume) * (2.0 * Math.PI);

                        for (double theta = radianStart; theta <= endRadians; theta += radiansStep)
                        {
                            thetas.Add(theta);
                        }

                        // convert radians to cartesian
                        (double[] xs, double[] ys) = ScottPlot.Tools.ConvertPolarCoordinates(thetas.Select(p => radius).ToArray(), thetas.ToArray());

                        List<(double x, double y)> polygonPoints = new List<(double x, double y)>();

                        polygonPoints.Add(centerOfCircle);
                        for (int i = 0; i < xs.Length; i++)
                        {
                            polygonPoints.Add((xs[i] + centerOfCircle.x, ys[i] + centerOfCircle.y));
                        }

                        var poly = new Polygon(polygonPoints.Select(p => p.x).ToArray(), polygonPoints.Select(p => p.y).ToArray());
                        poly.LineWidth = 0;
                        polygons.Add(poly);

                        radianStart = endRadians;
                    }

                    //TODO: figure out what to do for >2 quantitative values
                    ((Polygon)polygons[0]).FillColor = Color.FromArgb(252, 241, 115); // yellow
                    ((Polygon)polygons[1]).FillColor = Color.FromArgb(23, 191, 240); // blue

                    if (IsStatisticallySignificantlyDifferent)
                    {
                        // add an orange ring around the proteoform
                        List<double> thetas = new List<double>();

                        for (double theta = 0; theta <= 2.0 * Math.PI; theta += radiansStep)
                        {
                            thetas.Add(theta);
                        }

                        // convert radians to cartesian
                        (double[] xs, double[] ys) = ScottPlot.Tools.ConvertPolarCoordinates(thetas.Select(p => radius).ToArray(), thetas.ToArray());
                        var scatter = new ScatterPlot(xs.Select(p => p + centerOfCircle.x).ToArray(), ys.Select(p => p + centerOfCircle.y).ToArray());
                        scatter.LineWidth = 10;
                        scatter.MarkerSize = 0;
                        scatter.Color = Color.FromArgb(243, 112, 84); // orange
                        polygons.Add(scatter);
                    }

                    Node.VisualRepresentation = new List<IPlottable>(polygons);

                    break;
            }
        }
    }
}
