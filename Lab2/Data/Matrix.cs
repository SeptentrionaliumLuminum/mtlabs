using System;

namespace Lab2.Data
{
    [Serializable]
    public struct Matrix
    {
        public double[,] matrix { get; set; }

        public Matrix(int height, int width)
        {
            this.Height = height;
            this.Width = width;

            matrix = new double[height, width];
        }
        
        public int Height { get; }

        public int Width { get; }

        public double this[int row, int column]
        {
            get { return matrix[row, column]; }
            set { matrix[row, column] = value; }
        }

        public double[] GetColumn(int column)
        {
            var values = new double[Height];

            for (int row = 0; row < Height; row++)
            {
                values[row] = matrix[row, column];
            }

            return values;
        }

        public static Matrix Cut(Matrix source, int height, int width)
        {
            var matrix = new Matrix(height, width);

            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    matrix[row, column] = source[row, column];
                }
            }

            return matrix;
        }

        public static Matrix TakeColumn(Matrix source, int column)
        {
            var matrix = new Matrix(source.Height, 1);

            for (int row = 0; row < source.Height; row++)
            {
                matrix[row, 0] = source[row, column];
            }

            return matrix;
        }

    }
}
