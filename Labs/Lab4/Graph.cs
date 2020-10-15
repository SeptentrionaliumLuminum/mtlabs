using System;
using System.Collections.Generic;

namespace Lab4
{
    internal class Edge
    {
        public Edge(int from, int to, double weight)
        {
            From = from;
            To = to;

            Distance = weight;
        }

        public int From { get; }

        public int To { get; }

        public double Distance { get; }
    }

    internal class Graph
    {
        public const double MissingEdge = double.MinValue;

        public Graph(double[][] matrix)
        {
            this.Matrix = matrix ?? throw new ArgumentNullException(nameof(matrix));
        }

        public double[][] Matrix { get; }

        public int VertexesCount => Matrix.Length;

        public List<Edge> GetEdgesFrom(int vertex)
        {
            var edges = new List<Edge>();

            var rawEdges = Matrix[vertex];
            for (int index = 0; index < rawEdges.Length; index++)
            {
                if (rawEdges[index] != MissingEdge)
                    edges.Add(new Edge(vertex, index, rawEdges[index]));
            }

            return edges;
        }
    }
}
