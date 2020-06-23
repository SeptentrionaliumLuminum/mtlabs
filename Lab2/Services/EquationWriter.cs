using System.IO;

namespace Lab2.Services
{
    public static class EquationWriter
    {
        public static void Write(double[] result, string outputFileName)
        {
            using (StreamWriter writer = new StreamWriter(File.Open(outputFileName, FileMode.OpenOrCreate)))
            {
                writer.WriteLine(result.Length);

                foreach (var number in result)
                    writer.WriteLine(number);
            }
        }
    }
}
