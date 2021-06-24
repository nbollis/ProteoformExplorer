using System;
using System.Collections.Generic;
using System.Text;
using ScottPlot;
using ScottPlot.Plottable;

namespace ProteoformExplorer.ProteoformExplorerGUI
{
    public class Edge
    {
        public List<Node> ConnectedNodes { get; private set; }
        public double Weight { get; private set; }
        public ScatterPlot VisualRepresentation;
        public Text TextAnnotation;

        public Edge(Node node1, Node node2, string textAnnotation)
        {
            ConnectedNodes = new List<Node> { node1, node2 };

            VisualRepresentation = new ScatterPlot(new double[] { node1.X, node2.X }, new double[] { node1.Y, node2.Y });
            VisualRepresentation.LineWidth = 1;
            VisualRepresentation.MarkerSize = 0;

            TextAnnotation = new Text();
            TextAnnotation.X = (node1.X + node2.X) / 2.0;
            TextAnnotation.Y = (node1.Y + node2.Y) / 2.0;
            TextAnnotation.Label = textAnnotation;
        }
    }
}
