using System;
using System.Collections.Generic;
using System.Linq;

using Common;

using Lab3.Services;

namespace Lab3
{
    internal class Program
    {
        internal static void Main(string[] args)
        {
            //ReaderWriter.Generate(args[0], 10241024);
            //MultiThreaded(args);
            MultiThreadedVol2(args);
        }

        private static void MultiThreadedVol2(string[] args)
        {
            MPI.Environment.Run(ref args, communicator =>
            {
                //Initialize
                var inputFile = args[0];
                var outputFile = args[1];

                var sorter = new QSorter(communicator);
                var isManager = communicator.Rank == 0;

                int[] result = null;

                //Send array parts to workers
                if (isManager)
                {
                    var array = ReaderWriter.Read(inputFile);
                    var qsArray = new QSArray(array);

                    sorter.InitializeWithData(qsArray);
                }
                else
                {
                    sorter.InitializeWithData(null);
                }

                //Parallel QSort
                using (var performanceCounter = new PerformanceCounter($"Execution time [{communicator.Rank}]: "))
                {
                    while (true)
                    {
                        if (sorter.LastInGroup)
                        {
                            sorter.Sort();
                            break;
                        }

                        sorter.PivotBroadcast();
                        sorter.PartitionAndPartsExchange();
                        sorter.GroupHalfToSubGroup();
                    }

                    sorter.SendWorkResult();

                    //Collect all parts together
                    if (isManager)
                    {
                        result = sorter.MergeDataFromWorkers();
                    }
                }

                //Write to output file
                if (isManager)
                {
                    var list = result.ToList();
                    ReaderWriter.Write(outputFile, list);

                    //Console.WriteLine($"Verified: {Verify(list, inputFile)}");
                }
                
            });
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
            QuickSort.PivotType = PivotType.Middle;

            var array = ReaderWriter.Read(inputFile);
            QuickSort.Sort(array, 0, array.Count - 1);

            QuickSort.PivotType = PivotType.Middle;

            var flag = true;

            for (int index = 0; index < result.Count; index++)
            {
                //Console.WriteLine($"Index {index}: {result[index]} ({array[index]})");
                    
                if (result[index] != array[index])
                    flag = false;
            }

            return flag;
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
