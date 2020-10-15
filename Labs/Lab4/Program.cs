using System;
using System.IO;

namespace Lab4
{
    class Program
    {
        static void Main(string[] args)
        {
            //Test for correctness:
            //CorrectnessTest();

            //Create random graph, compare single-thread and multithread results:
            //RandomGraphTest(15000);

            TestsWithFileInputOutput(args);
        }

        private static void RandomGraphTest(int numberOfVertexes)
        {
            var randomGraph = GraphBuilder.BuildRandomGraph(numberOfVertexes);

            RunTests(randomGraph, verifyResults: true);

            Console.ReadKey();
        }

        static void TestsWithFileInputOutput(string[] args)
        {
            var inputFileName = args[0];
            var outputFileName = args[1];

            var graph = GraphBuilder.LoadFromFile(inputFileName);

            RunTests(graph, verifyResults: true, outputToFile: true, outputFile: outputFileName);

            Console.ReadKey();
        }

        static void CorrectnessTest()
        {
            //Build graph
            var graphBuilder = new GraphBuilder(6);
            graphBuilder.AddEdge(0, 1, 7);
            graphBuilder.AddEdge(0, 2, 9);
            graphBuilder.AddEdge(0, 5, 14);
            graphBuilder.AddEdge(1, 3, 15);
            graphBuilder.AddEdge(1, 2, 10);
            graphBuilder.AddEdge(5, 2, 2);
            graphBuilder.AddEdge(2, 3, 11);
            graphBuilder.AddEdge(5, 4, 9);
            graphBuilder.AddEdge(4, 3, 6);

            var graph = graphBuilder.BuildGraph();

            //Correct distances for this graph (vertex - distance):
            //1 - 7, 2 - 9, 3 - 20, 4 - 20, 5 - 11

            RunTests(graph, outputToConsole: true, verifyResults: true);

            Console.ReadKey();
        }

        private static void RunTests(Graph graph, 
            int maxThreads = 16,
            bool outputToConsole = false, 
            bool outputToFile = false, string outputFile = null,
            bool verifyResults = false)
        {
            //Single thread
            var singleThreadedDijkstra = new Dijkstra(graph, 1);
            double[] singleThreadedResult = null;

            using (var performanceCounter = new Common.PerformanceCounter($"Single-threaded:"))
            {
                singleThreadedResult = singleThreadedDijkstra.RunInSingleThreadMode(0);
            }
            
            if (outputToConsole)
                OutputDistancesInConsole(singleThreadedResult);

            //Multi thread
            for (int threadsNumber = 1; threadsNumber <= maxThreads; threadsNumber *= 2)
            {
                var multiThreadedDijkstra = new Dijkstra(graph, threadsNumber);
                double[] multiThreadedResult = null;

                using (var performanceCounter = new Common.PerformanceCounter($"1 + {threadsNumber} ({1 + threadsNumber}): "))
                {
                    multiThreadedResult = multiThreadedDijkstra.FindDistancesForVertex(0);
                }

                if (outputToConsole)
                    OutputDistancesInConsole(multiThreadedResult);

                if (outputToFile)
                    WriteDistancesToFile(multiThreadedResult, outputFile);

                if (verifyResults)
                {
                    var verificationResult = Verify(multiThreadedResult, singleThreadedResult);
                    Console.WriteLine($"Verified: {verificationResult}");
                }
            }
        }

        private static bool Verify(double[] testDistances, double[] distancesToBe)
        {

            if (testDistances.Length != distancesToBe.Length)
                return false;

            for (int index = 0; index < testDistances.Length; index++)
            {
                if (testDistances[index] != distancesToBe[index])
                    return false;
            }

            return true;
        }

        private static void WriteDistancesToFile(double[] distances, string outputFilename)
        {
            var output = "";
            for (int index = 0; index < distances.Length; index++)
            {
                var distance = distances[index];
                if (distance == Graph.MissingEdge)
                    output += "INF ";
                else
                    output += $"{distance} ";
            }

            File.WriteAllText(outputFilename, output);
        }

        private static void OutputDistancesInConsole(double[] distances)
        {
            for (int index = 0; index < distances.Length; index++)
            {
                Console.WriteLine($"{index} - {distances[index]}");
            }
        }
    }
}
