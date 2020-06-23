using System.IO;
using System.Linq;

using Lab2.Data;

namespace Lab2.Services
{
    public static class EquationReader
    {
        public static Jacoby ReadJacoby(string matrixFile, string initialFile, double precision)
        {
            var matrix = ReadMatrix(matrixFile);
            var vector = ReadMatrix(initialFile);

            return new Jacoby(matrix, vector, precision);
        }

        private static Matrix ReadMatrix(string matrixFile)
        {
            using (StreamReader reader = new StreamReader(File.Open(matrixFile, FileMode.Open)))
            {
                var matrix = CreateEmptyMatrixByHeightAndWidth(reader);
                FillMatrixWithData(matrix, reader);

                return matrix;
            }
        }

        private static void FillMatrixWithData(Matrix matrix, StreamReader reader)
        {
            var row = 0;
            while (!reader.EndOfStream)
            {
                var values = GetDoubleNumbers(reader);

                for (int column = 0; column < values.Length; column++)
                {
                    matrix[row, column] = values[column];
                }

                row++;
            }
        }

        private static Matrix CreateEmptyMatrixByHeightAndWidth(StreamReader reader)
        {
            var heightAndWidth = GetIntNumbers(reader);
            var height = heightAndWidth[0];

            var width = heightAndWidth.Length == 1 ? 1 : heightAndWidth[1];

            var matrix = new Matrix(height, width);

            return matrix;
        }

        private static int[] GetIntNumbers(StreamReader reader)
        {
            var line = reader.ReadLine();
            var fragments = line.Split(' ');

            var number = fragments.Select(fragment => int.Parse(fragment)).ToArray();

            return number;
        }

        private static double[] GetDoubleNumbers(StreamReader reader)
        {
            var line = reader.ReadLine();
            var fragments = line.Split(' ');

            var number = fragments.Select(fragment => double.Parse(fragment)).ToArray();

            return number;
        }
    }
}
