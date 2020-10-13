using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lab3.Services
{
    public static class ReaderWriter
    {
        public static List<int> Read(string inputFile)
        {
            var inputString = File.ReadAllText(inputFile);
            var splittedString = inputString.Split(' ');

            splittedString = splittedString.Where(el => !string.IsNullOrWhiteSpace(el)).ToArray();

            return splittedString.Select(subString => int.Parse(subString)).ToList();
        }

        internal static void Write(string outputFile, IList<int> result)
        {
            var stringBuilder = new StringBuilder();
            foreach (var el in result)
            {
                stringBuilder.Append(' ');
                stringBuilder.Append(el.ToString());
            }

            File.WriteAllText(outputFile, stringBuilder.ToString());
        }

        internal static void Generate(string outputFile, int size)
        {
            var random = new Random();

            var list = new List<int>();
            for (int index = 0; index < size; index++)
            {
                var value = random.Next(0, size);
                list.Add(value);
            }

            Write(outputFile, list);
        }
    }
}
