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

            var exampleFamily = new List<VisualizedProteoformFamilyMember> 
            {
                new VisualizedProteoformFamilyMember(VisualizedType.Gene, @"TestGene", 10, 0),
                new VisualizedProteoformFamilyMember(VisualizedType.TheoreticalProteoform, @"Unmodified", 15, 5),
                new VisualizedProteoformFamilyMember(VisualizedType.TopDownExperimentalProteoform, @"Unmodified", 15, 10),
                new VisualizedProteoformFamilyMember(VisualizedType.IntactMassExperimentalProteoform, @"Acetyl", 5, 5),
            };

            exampleFamily[0].Node.AddConnection(exampleFamily[1].Node, "0 Da");
            exampleFamily[0].Node.AddConnection(exampleFamily[2].Node, "0 Da");
            exampleFamily[1].Node.AddConnection(exampleFamily[3].Node, "42 Da");

            DrawProteoformFamily(exampleFamily);
        }

        public void DrawProteoformFamily(List<VisualizedProteoformFamilyMember> proteoformFamily)
        {
            pfmFamilyVisualizationChart.Plot.Grid(false);

            foreach (var proteoform in proteoformFamily)
            {
                foreach (var connection in proteoform.Node.Edges)
                {
                    connection.VisualRepresentation.LineWidth = 1;
                    connection.VisualRepresentation.Color = Color.Black;
                    connection.TextAnnotation.Color = Color.Black;

                    pfmFamilyVisualizationChart.Plot.Add(connection.VisualRepresentation);
                    pfmFamilyVisualizationChart.Plot.Add(connection.TextAnnotation);
                }
            }

            foreach (var proteoform in proteoformFamily)
            {
                pfmFamilyVisualizationChart.Plot.Add(proteoform.Node.VisualRepresentation);
                pfmFamilyVisualizationChart.Plot.Add(proteoform.Node.TextAnnotation);
            }
        }
    }

    public class VisualizedProteoformFamilyMember
    {
        public VisualizedType Type { get; private set; }
        public Node Node { get; private set; }

        public VisualizedProteoformFamilyMember(VisualizedType type, string nodeAnnotation, double x, double y)
        {
            Type = type;
            Node = new Node(x, y, nodeAnnotation);
            BuildProteoformVisualRepresentation();
        }

        private void BuildProteoformVisualRepresentation()
        {
            Node.VisualRepresentation.MarkerSize = 70;

            switch (Type)
            {
                case VisualizedType.Gene:
                    Node.VisualRepresentation.MarkerShape = ScottPlot.MarkerShape.filledDiamond;
                    Node.VisualRepresentation.Color = Color.DeepPink;
                    break;

                case VisualizedType.TheoreticalProteoform:
                    Node.VisualRepresentation.MarkerShape = ScottPlot.MarkerShape.filledCircle;
                    Node.VisualRepresentation.Color = Color.LimeGreen;
                    break;

                case VisualizedType.TopDownExperimentalProteoform:
                    Node.VisualRepresentation.MarkerShape = ScottPlot.MarkerShape.filledCircle;
                    Node.VisualRepresentation.Color = Color.SlateBlue;
                    break;

                case VisualizedType.IntactMassExperimentalProteoform:
                    Node.VisualRepresentation.MarkerShape = ScottPlot.MarkerShape.filledCircle;
                    Node.VisualRepresentation.Color = Color.DodgerBlue;
                    break;
            }
        }
    }
}
