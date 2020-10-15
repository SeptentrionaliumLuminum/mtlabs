using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Lab4
{
    /// <summary>
    /// Constructs Graph, save/load it from file
    /// </summary>
    internal class GraphBuilder
    {
        private double[][] matrix;

        public GraphBuilder()
        {

        }

        public GraphBuilder(int numberOfVertexes)
        {
            this.matrix = CreateMatrix(numberOfVertexes);
        }

        public void AddEdge(int from, int to, double distance)
        {
            matrix[from][to] = distance;
            matrix[to][from] = distance;
        }

        public Graph BuildGraph()
        {
            return new Graph(matrix);
        }

        public static double[][] CreateMatrix(int numberOfVertexes)
        {
            var matrix = new double[numberOfVertexes][];
            for (int i = 0; i < numberOfVertexes; i++)
            {
                matrix[i] = new double[numberOfVertexes];
                for (int j = 0; j < numberOfVertexes; j++)
                {
                    if (i == j)
                        matrix[i][j] = 0;
                    else
                        matrix[i][j] = Graph.MissingEdge;
                }
            }

            return matrix;
        }

        public static Graph LoadFromFile(string fileName)
        {
            var rows = File.ReadAllLines(fileName);

            var matrix = CreateMatrix(rows.Length);
            for (int row = 0; row < rows.Length; row++)
            {
                var splitted = rows[row].Split(' ').Where(el => !string.IsNullOrWhiteSpace(el)).Select(el => double.Parse(el)).ToList();
                for (int column = 0; column < rows.Length; column++)
                {
                    matrix[row][column] = splitted[column];
                }
            }

            return new Graph(matrix);
        }


        public static void WriteGraph(Graph graph, string outputFile)
        {
            var matrix = graph.Matrix;

            StringBuilder stringBuilder = new StringBuilder();
            for (int row = 0; row < matrix.Length; row++)
            {
                for (int column = 0; column < matrix.Length; column++)
                {
                    stringBuilder.Append($"{matrix[row][column]} ");
                }

                if (row != matrix.Length - 1)
                    stringBuilder.Append(Environment.NewLine);
            }

            File.WriteAllText(outputFile, stringBuilder.ToString());
        }
        
        public static Graph BuildRandomGraph(int numberOfVertexes)
        {
            var matrix = CreateMatrix(numberOfVertexes);

            var random = new Random();

            for (int row = 0; row < numberOfVertexes; row++)
            {
                for (int column = 0; column < numberOfVertexes; column++)
                {
                    if (row == column)
                        continue;

                    bool isEdgeExist = true;//random.NextDouble() >= 0.5f;
                    if (isEdgeExist)
                    {
                        var distance = random.NextDouble() * 1000;
                        matrix[row][column] = distance;
                    }
                }
            }

            return new Graph(matrix);
        }
    }
}
