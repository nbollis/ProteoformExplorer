using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using ScottPlot;
using ScottPlot.Plottable;

namespace GUI
{
    public class Node
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        private List<Edge> _edges;
        public List<IPlottable> VisualRepresentation { get; set; }
        public Text TextAnnotation;

        public Node(double x, double y, string textAnnotation = null)
        {
            this.X = x;
            this.Y = y;
            _edges = new List<Edge>();

            VisualRepresentation = new List<IPlottable> { new ScatterPlot(new double[] { x }, new double[] { y }) };

            TextAnnotation = new Text();
            TextAnnotation.X = X;
            TextAnnotation.Y = Y;
            TextAnnotation.Label = textAnnotation;
        }

        public void AddConnection(Node connectedNode, string edgeAnnotation, bool bidirectional = true)
        {
            var edge = new Edge(this, connectedNode, edgeAnnotation);

            this._edges.Add(edge);

            if (bidirectional)
            {
                connectedNode.AddConnection(this, edgeAnnotation, false);
            }
        }

        public bool RemoveConnection(Node connectedNode)
        {
            var edgeToRemove = Edges.FirstOrDefault(p => p.ConnectedNodes.Contains(connectedNode));

            if (edgeToRemove != null)
            {
                _edges.Remove(edgeToRemove);
                return true;
            }

            return false;
        }

        public IEnumerable<Edge> Edges
        {
            get
            {
                return _edges;
            }
        }

        public IEnumerable<Node> ConnectedNodes()
        {
            foreach (var edge in Edges)
            {
                foreach (var node in edge.ConnectedNodes)
                {
                    if (this != node)
                    {
                        yield return node;
                    }
                }
            }
        }
    }
}
