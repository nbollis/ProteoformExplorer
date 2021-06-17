using ProteoformExplorer;
using ScottPlot.Plottable;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace GUI.Pages
{
    public enum VisualizedType { Gene, TheoreticalProteoform, TopDownExperimentalProteoform, IntactMassExperimentalProteoform }

    /// <summary>
    /// Interaction logic for ProteoformFamilyVisualization.xaml
    /// </summary>
    public partial class ProteoformFamilyVisualization : Page
    {
        public ProteoformFamilyVisualization()
        {
            InitializeComponent();

            var exampleFamily = new List<VisualizedProteoformFamilyMember>();

            exampleFamily.Add(new VisualizedProteoformFamilyMember(VisualizedType.Gene, @"TestGene", 10, 0));
            exampleFamily.Add(new VisualizedProteoformFamilyMember(VisualizedType.TheoreticalProteoform, @"Unmodified", 15, 5));
            exampleFamily.Add(new VisualizedProteoformFamilyMember(VisualizedType.TopDownExperimentalProteoform, @"Unmodified", 15, 10));
            exampleFamily.Add(new VisualizedProteoformFamilyMember(VisualizedType.IntactMassExperimentalProteoform, @"Acetyl", 5, 5));

            exampleFamily[0].Connections.Add(("0 Da", exampleFamily[1]));
            exampleFamily[0].Connections.Add(("0 Da", exampleFamily[2]));
            exampleFamily[1].Connections.Add(("42 Da", exampleFamily[3]));

            DrawProteoformFamily(exampleFamily);
        }

        public void DrawProteoformFamily(List<VisualizedProteoformFamilyMember> proteoformFamily)
        {
            pfmFamilyVisualizationChart.Plot.Grid(false);

            foreach (var proteoform in proteoformFamily)
            {
                foreach (var connection in proteoform.Connections)
                {
                    pfmFamilyVisualizationChart.Plot.AddLine(proteoform.x, proteoform.y, connection.Item2.x, connection.Item2.y, Color.Black, lineWidth: 1);
                    pfmFamilyVisualizationChart.Plot.AddText(connection.edgeAnnotation, (proteoform.x + connection.Item2.x) / 2.0, (proteoform.y + connection.Item2.y) / 2.0, 12, Color.Black);
                }
            }

            foreach (var proteoform in proteoformFamily)
            {
                pfmFamilyVisualizationChart.Plot.Add(proteoform.Graphic);
                pfmFamilyVisualizationChart.Plot.AddText(proteoform.NodeAnnotation, proteoform.x, proteoform.y, 12, Color.Black);
            }
        }
    }

    public class VisualizedProteoformFamilyMember
    {
        public readonly string NodeAnnotation;
        public IPlottable Graphic { get; private set; }
        public List<(string edgeAnnotation, VisualizedProteoformFamilyMember)> Connections { get; set; }
        public VisualizedType Type { get; private set; }
        public double x;
        public double y;

        public VisualizedProteoformFamilyMember(VisualizedType type, string nodeAnnotation, double x, double y)
        {
            Type = type;
            NodeAnnotation = nodeAnnotation;
            this.x = x;
            this.y = y;
            Connections = new List<(string edgeAnnotation, VisualizedProteoformFamilyMember)>();
            BuildProteoformVisualRepresentation();
        }

        private void BuildProteoformVisualRepresentation()
        {
            ScatterPlot temp = new ScatterPlot(new double[] { x }, new double[] { y });
            temp.MarkerSize = 70;
            temp.LineWidth = 0;
            Graphic = temp;

            switch (Type)
            {
                case VisualizedType.Gene:
                    temp.MarkerShape = ScottPlot.MarkerShape.filledDiamond;
                    temp.Color = Color.DeepPink;
                    break;

                case VisualizedType.TheoreticalProteoform:
                    temp.MarkerShape = ScottPlot.MarkerShape.filledCircle;
                    temp.Color = Color.LimeGreen;
                    break;

                case VisualizedType.TopDownExperimentalProteoform:
                    temp.MarkerShape = ScottPlot.MarkerShape.filledCircle;
                    temp.Color = Color.SlateBlue;
                    break;

                case VisualizedType.IntactMassExperimentalProteoform:
                    temp.MarkerShape = ScottPlot.MarkerShape.filledCircle;
                    temp.Color = Color.DodgerBlue;
                    break;
            }
        }
    }
}
