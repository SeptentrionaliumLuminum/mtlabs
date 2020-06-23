using System;
using System.IO;

namespace Lab2.Services
{
    public static class EquationGenerator
    {
        public static void Generate(string fileName, string first, int size)
        {
            var random = new Random();

            int[] xs = new int[size];
            for (int i = 0; i < size; i++)
                xs[i] = random.Next(-100, 100);

            using (StreamWriter writer = new StreamWriter(File.Open(first, FileMode.Open)))
            {
                writer.WriteLine(size);
                for (int i = 0; i < size; i++)
                    writer.WriteLine(0);
            }

            using (StreamWriter writer = new StreamWriter(File.Open(fileName, FileMode.OpenOrCreate)))
            {
                writer.WriteLine($"{size} {size + 1}");

                for (int row = 0; row < size; row++)
                {
                    var values = new int[size];
                    var result = 0;

                    for (int column = 0; column < size; column++)
                    {
                        values[column] = random.Next(-50, 50);
                        result += xs[column] * values[column];
                        writer.Write($"{values[column]} ");
                    }

                    writer.WriteLine($"{result}");
                }
            }
        }
    }
}
