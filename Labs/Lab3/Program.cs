using System;
using System.Collections.Generic;

using Common;

using Lab3.Services;

namespace Lab3
{
    internal class Program
    {
        internal static void Main(string[] args)
        {
            //ReaderWriter.Generate(args[0], 40960000);
            MultiThreaded(args);
        }

        private static void MultiThreaded(string[] args)
        {
            MPI.Environment.Run(ref args, communicator =>
            {
                var sorter = new MPIQuickSort(communicator);
                var isMaster = communicator.Rank == 0;

                if (isMaster)
                {
                    var inputFile = args[0];
                    var outputFile = args[1];

                    var array = ReaderWriter.Read(inputFile);

                    IList<int> result = null;

                    using (var performanceCounter = new PerformanceCounter($"Execution time: "))
                    {
                        result = sorter.ParallelSort(array, 0, array.Count - 1);
                    }

                    ReaderWriter.Write(outputFile, result);

                    //Console.WriteLine($"Verify result: {Verify(result, inputFile)}");
                }
                else
                {
                    sorter.WorkerLoop();
                }
            });
        }

        private static bool Verify(IList<int> result, string inputFile)
        {
            var array = ReaderWriter.Read(inputFile);
            QuickSort.Sort(array, 0, array.Count - 1);

            for (int index = 0; index < result.Count; index++)
                if (result[index] != array[index])
                    return false;

            return true;
        }

        private static void SingleThreaded(string[] args)
        {
            var inputFile = args[0];
            var outputFile = args[1];

            var array = ReaderWriter.Read(inputFile);
            QuickSort.Sort(array, 0, array.Count - 1);

            ReaderWriter.Write(outputFile, array);
        }

        private static void SingleThreadedTest()
        {
            var array = new List<int> { 0, -6, 3, 23, 1, 4 };
            QuickSort.Sort(array, 0, array.Count - 1);

            array.ForEach(el => Console.WriteLine(el));
            Console.ReadKey();
        }
    }
}
