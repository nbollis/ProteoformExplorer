using ProteoformExplorer.Core;
using ProteoformExplorer.GuiFunctions;
using ScottPlot.Plottable;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProteoformExplorer.Wpf
{
    public enum VisualizedType { Gene, Transcript, TheoreticalProteoform, TopDownExperimentalProteoform, IntactMassExperimentalProteoform, QuantifiedExperimentalProteoform }

    /// <summary>
    /// Interaction logic for ProteoformFamilyVisualization.xaml
    /// </summary>
    public partial class ProteoformFamilyVisualization : Page
    {
        private static List<VisualizedProteoformFamilyMember> AllVisualizedProteoforms;

        public ProteoformFamilyVisualization()
        {
            InitializeComponent();

            // this gets DPI scaling info
            GuiFunctions.GuiFunctions.StylePlot(pfmFamilyVisualizationChart.Plot);

            var exampleFamily = new List<VisualizedProteoformFamilyMember>
            {
                new VisualizedProteoformFamilyMember(VisualizedType.Gene, @"TestGene", 10, 0, null),
                new VisualizedProteoformFamilyMember(VisualizedType.TheoreticalProteoform, @"Unmodified", 15, 5, null),
                new VisualizedProteoformFamilyMember(VisualizedType.TopDownExperimentalProteoform, @"Unmodified", 15, 10, null),
                new VisualizedProteoformFamilyMember(VisualizedType.IntactMassExperimentalProteoform, @"Acetyl", 5, 5, null),
                new VisualizedProteoformFamilyMember(VisualizedType.QuantifiedExperimentalProteoform, @"Unmodified", 7, 7, null,
                    new List<double> { 1.0, 2.0 }, isStatisticallySignificant: true),
            };

            exampleFamily[0].Node.AddConnection(exampleFamily[1].Node, "0 Da");
            exampleFamily[0].Node.AddConnection(exampleFamily[2].Node, "0 Da");
            exampleFamily[1].Node.AddConnection(exampleFamily[4].Node, "0 Da");
            exampleFamily[1].Node.AddConnection(exampleFamily[3].Node, "42 Da");

            DrawProteoformFamily(exampleFamily);

            AllVisualizedProteoforms = exampleFamily;

            // unsubscribe from the default right-click menu event
            pfmFamilyVisualizationChart.RightClicked -= pfmFamilyVisualizationChart.DefaultRightClickEvent;
            pfmFamilyVisualizationChart.Configuration.RightClickDragZoom = false;

            // add custom right-click event
            //pfmFamilyVisualizationChart.RightClicked += DeployCustomMenu;
        }

        public void DrawProteoformFamily(List<VisualizedProteoformFamilyMember> proteoformFamily)
        {
            pfmFamilyVisualizationChart.Plot.AxisScaleLock(true);
            pfmFamilyVisualizationChart.Plot.Grid(GuiSettings.ShowChartGrid);
            pfmFamilyVisualizationChart.Plot.Frameless();

            foreach (var proteoform in proteoformFamily)
            {
                foreach (var connection in proteoform.Node.Edges)
                {
                    connection.VisualRepresentation.LineWidth = GuiSettings.ChartLineWidth * GuiSettings.DpiScalingX;
                    connection.VisualRepresentation.Color = Color.FromArgb(124, 124, 124); // gray
                    connection.TextAnnotation.Color = Color.FromArgb(255, 113, 79); // orange text annotation for edge
                    connection.TextAnnotation.FontSize = (float)(GuiSettings.ChartLabelFontSize * GuiSettings.DpiScalingX);

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

            // get the item that was clicked
            //PfmXplorerUtil.GetXPositionFromMouseClickOnChart(sender, (MouseButtonEventArgs) e);

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

        private void pfmFamilyVisualizationChart_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            int pixelX = (int)e.MouseDevice.GetPosition(pfmFamilyVisualizationChart).X;
            int pixelY = (int)e.MouseDevice.GetPosition(pfmFamilyVisualizationChart).Y;

            (double coordinateX, double coordinateY) = pfmFamilyVisualizationChart.GetMouseCoordinates();

            var closestPfm = AllVisualizedProteoforms
                .OrderBy(p => Math.Sqrt(Math.Pow(coordinateX - p.Node.X, 2) + Math.Pow(coordinateY - p.Node.Y, 2)))
                .FirstOrDefault();

            if (closestPfm == null
                || closestPfm.Type == VisualizedType.Gene
                || closestPfm.Type == VisualizedType.Transcript
                || closestPfm.Type == VisualizedType.TheoreticalProteoform)
            {
                return;
            }

            var nodeItems = closestPfm.Node.VisualRepresentation.Where(p => p is Polygon).ToList();

            // get the radius
            double radius = double.NegativeInfinity;
            foreach (var item in nodeItems)
            {
                var poly = (Polygon)item;
                radius = Math.Max(radius, Math.Abs(poly.Ys.Max() - closestPfm.Node.Y));
            }

            bool clickLocationIsInsideCircle = Math.Sqrt(Math.Pow(coordinateX - closestPfm.Node.X, 2) + Math.Pow(coordinateY - closestPfm.Node.Y, 2)) < radius;

            if (clickLocationIsInsideCircle)
            {
                // this is just temporary. TODO: show right-click menu to see XIC plotting options
                foreach (var item in nodeItems)
                {
                    var poly = (Polygon)item;
                    poly.HatchStyle = ScottPlot.Drawing.HatchStyle.StripedDownwardDiagonal;
                }
            }
        }
    }

    public class VisualizedProteoformFamilyMember
    {
        public VisualizedType Type { get; private set; }
        public Node Node { get; private set; }
        public List<double> QuantifiedIntensities { get; private set; }
        public bool IsStatisticallySignificantlyDifferent { get; private set; }

        public AnnotatedSpecies AnnotatedSpecies { get; private set; }

        public VisualizedProteoformFamilyMember(VisualizedType type, string nodeAnnotation, double x, double y, AnnotatedSpecies species,
            List<double> quantifiedIntensities = null, bool isStatisticallySignificant = false)
        {
            Type = type;
            Node = new Node(x, y, nodeAnnotation);
            QuantifiedIntensities = quantifiedIntensities;
            IsStatisticallySignificantlyDifferent = isStatisticallySignificant;
            AnnotatedSpecies = species;
            BuildProteoformVisualRepresentation();
        }

        private void BuildProteoformVisualRepresentation()
        {
            Node.TextAnnotation.Color = Color.Black; // black text annotation for node
            Node.TextAnnotation.FontSize = (float)(GuiSettings.ChartLabelFontSize * GuiSettings.DpiScalingX);

            switch (Type)
            {
                case VisualizedType.Gene:
                    var qualitativeMarker = GetSquare(Node.X, Node.Y, 2, Color.FromArgb(255, 116, 175)); // pink
                    Node.VisualRepresentation = new List<IPlottable> { qualitativeMarker };
                    break;

                case VisualizedType.Transcript:
                    qualitativeMarker = GetDiamond(Node.X, Node.Y, 2, Color.FromArgb(255, 116, 175)); // pink
                    Node.VisualRepresentation = new List<IPlottable> { qualitativeMarker };
                    break;

                case VisualizedType.TheoreticalProteoform:
                    qualitativeMarker = GetSemiCircle(0, 2 * Math.PI, Node.X, Node.Y, 1, Color.FromArgb(0, 183, 62)); // green
                    Node.VisualRepresentation = new List<IPlottable> { qualitativeMarker };
                    break;

                case VisualizedType.TopDownExperimentalProteoform:
                    qualitativeMarker = GetSemiCircle(0, 2 * Math.PI, Node.X, Node.Y, 1, Color.FromArgb(144, 123, 189)); // purple
                    Node.VisualRepresentation = new List<IPlottable> { qualitativeMarker };
                    break;

                case VisualizedType.IntactMassExperimentalProteoform:
                    qualitativeMarker = GetSemiCircle(0, 2 * Math.PI, Node.X, Node.Y, 1, Color.FromArgb(0, 193, 245)); // blue
                    Node.VisualRepresentation = new List<IPlottable> { qualitativeMarker };
                    break;

                case VisualizedType.QuantifiedExperimentalProteoform:
                    List<IPlottable> polygons = new List<IPlottable>();
                    double volume = QuantifiedIntensities.Sum();
                    double radius = Math.Sqrt(volume / Math.PI);

                    double radianStart = Math.PI / 2.0;

                    foreach (var intensity in QuantifiedIntensities)
                    {
                        double endRadians = radianStart + (intensity / volume) * (2.0 * Math.PI);

                        var poly = GetSemiCircle(radianStart, endRadians, Node.X, Node.Y, radius, Color.White);
                        polygons.Add(poly);

                        radianStart = endRadians;
                    }

                    //TODO: figure out what to do for >2 quantitative values
                    ((Polygon)polygons[0]).FillColor = Color.FromArgb(252, 241, 115); // yellow
                    ((Polygon)polygons[1]).FillColor = Color.FromArgb(23, 191, 240); // blue

                    if (IsStatisticallySignificantlyDifferent)
                    {
                        // add an orange ring around the proteoform
                        var outline = GetSemiCircle(0, 2 * Math.PI, Node.X, Node.Y, radius, Color.FromArgb(243, 112, 84), radius * 0.9);

                        polygons.Add(outline);
                    }

                    Node.VisualRepresentation = new List<IPlottable>(polygons);

                    break;
            }
        }

        private Polygon GetSemiCircle(double radianStart, double radianEnd, double xCenter, double yCenter, double radius, Color color, double donutHoleSize = 0)
        {
            List<double> thetas = new List<double>();
            double radiansStep = 0.01;

            for (double theta = radianStart; theta <= radianEnd; theta += radiansStep)
            {
                thetas.Add(theta);
            }

            // convert radians to cartesian
            (double[] xs, double[] ys) = ScottPlot.Tools.ConvertPolarCoordinates(thetas.Select(p => radius).ToArray(), thetas.ToArray());

            List<(double x, double y)> polygonPoints = new List<(double x, double y)>();

            if (radianEnd % (2 * Math.PI) != radianStart)
            {
                polygonPoints.Add((xCenter, yCenter));
            }

            for (int i = 0; i < xs.Length; i++)
            {
                polygonPoints.Add((xs[i] + xCenter, ys[i] + yCenter));
            }

            if (donutHoleSize > 0)
            {
                thetas.Clear();

                for (double theta = radianEnd; theta >= radianStart; theta -= radiansStep)
                {
                    thetas.Add(theta);
                }

                (xs, ys) = ScottPlot.Tools.ConvertPolarCoordinates(thetas.Select(p => donutHoleSize).ToArray(), thetas.ToArray());
                for (int i = 0; i < xs.Length; i++)
                {
                    polygonPoints.Add((xs[i] + xCenter, ys[i] + yCenter));
                }
            }

            var poly = new Polygon(polygonPoints.Select(p => p.x).ToArray(), polygonPoints.Select(p => p.y).ToArray());
            poly.FillColor = color;
            poly.Fill = true;
            poly.LineWidth = 0;

            return poly;
        }

        private Polygon GetSquare(double xCenter, double yCenter, double sideLength, Color color)
        {
            var polygonPoints = new List<(double x, double y)>
            {
                (xCenter - sideLength / 2, yCenter - sideLength / 2),
                (xCenter + sideLength / 2, yCenter - sideLength / 2),
                (xCenter + sideLength / 2, yCenter + sideLength / 2),
                (xCenter - sideLength / 2, yCenter + sideLength / 2),
            };

            var poly = new Polygon(polygonPoints.Select(p => p.x).ToArray(), polygonPoints.Select(p => p.y).ToArray());
            poly.FillColor = color;
            poly.Fill = true;
            poly.LineWidth = 0;

            return poly;
        }

        private Polygon GetDiamond(double xCenter, double yCenter, double height, Color color)
        {
            var polygonPoints = new List<(double x, double y)>
            {
                (xCenter, yCenter - height / 2),
                (xCenter + height / 2, yCenter),
                (xCenter, yCenter + height / 2),
                (xCenter - height / 2, yCenter),
            };

            var poly = new Polygon(polygonPoints.Select(p => p.x).ToArray(), polygonPoints.Select(p => p.y).ToArray());
            poly.FillColor = color;
            poly.Fill = true;
            poly.LineWidth = 0;

            return poly;
        }
    }
}
