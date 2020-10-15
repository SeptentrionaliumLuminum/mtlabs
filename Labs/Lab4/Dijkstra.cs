using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lab4
{
    internal class Dijkstra
    {
        private class JobToDo
        {
            public bool Explore;
            public bool Min;
            public int PartNumber;
        }

        private readonly Graph graph;
        private readonly int threadsCount = 4;

        private readonly HashSet<int> visitedNodes = new HashSet<int>();

        private HashSet<int> nodesToVisit = new HashSet<int>();
        private List<int> nodesToVisitList;

        private double[] distances;

        private static IList<Edge> currentEdges;
        private static double currentDistance;

        private CustomThreadPool<JobToDo> threadPool;

        private static int[] indexes;
        private static EventWaitHandle[] waitHandlers;

        public Dijkstra(Graph graph, int threadsCount)
        {
            this.graph = graph ?? throw new ArgumentNullException(nameof(graph));
            this.threadsCount = threadsCount;
        }

        public double[] FindDistancesForVertex(int vertex)
        {
            Initialize(vertex);

            ParallelLoop();

            return distances;
        }

        public double[] RunInSingleThreadMode(int vertex)
        {
            Initialize(vertex);

            while (nodesToVisit.Any())
            {
                //using (var pctr = new PerformanceCounter($"Single-threaded loop: "))
                {
                    SingleVisit();
                }
            }

            return distances;
        }

        private void ParallelLoop()
        {
            using (var threads = CreateThreads())
            {
                while (nodesToVisit.Any())
                {
                    //using (var pctr = new PerformanceCounter($"Parallel loop {threadsCount}: "))
                    {
                        nodesToVisitList = nodesToVisit.ToList();

                        var minimalNode = FindMinimalVertexParallel();
                        nodesToVisit.Remove(minimalNode);
                        visitedNodes.Add(minimalNode);

                        currentEdges = graph.GetEdgesFrom(minimalNode);

                        currentDistance = distances[minimalNode];

                        //Main thread doesn't involve in ExploreEdgesParallel()
                        //Main thread find new nodes to visit
                        ExploreEdgesParallel();

                        var newEdges = currentEdges.Where(edge => !visitedNodes.Contains(edge.To) &&
                            !nodesToVisit.Contains(edge.To)).ToList();

                        foreach (var edge in newEdges)
                            nodesToVisit.Add(edge.To);

                        WaitHandle.WaitAll(waitHandlers);
                        ResetHandlers();
                    }
                }
            }
        }

        private CustomThreadPool<JobToDo> CreateThreads()
        {
            threadPool = new CustomThreadPool<JobToDo>(threadsCount,
                (job) =>
                {
                    if (job.Explore)
                        ExploreParallel(job.PartNumber);
                    else
                        WorkerFindMinimal(job.PartNumber);

                    waitHandlers[job.PartNumber].Set();
                });

            return threadPool;
        }

        private void ExploreEdgesParallel()
        {
            InitHandlers();

            for (int threadNumber = 0; threadNumber < threadsCount; threadNumber++)
            {
                var job = new JobToDo() { Explore = true, PartNumber = threadNumber };
                threadPool.EnqueueTask(job);
            }
        }

        private void ResetHandlers()
        {
            for (int index = 0; index < threadsCount; index++)
                waitHandlers[index].Reset();
        }

        private void InitHandlers()
        {
            waitHandlers = new EventWaitHandle[threadsCount];
            for (int index = 0; index < threadsCount; index++)
                waitHandlers[index] = new EventWaitHandle(false, EventResetMode.ManualReset);
        }

        private void ExploreParallel(int threadNumber)
        {
            var thread = threadNumber;

            var portion = currentEdges.Count / threadsCount;
            var from = thread * portion;
            var to = thread == threadsCount - 1 ? currentEdges.Count : (thread + 1) * portion;

            for (int index = from; index < to; index++)
            {
                ExploreEdge(currentEdges[index], currentDistance);
            }
        }

        private int FindMinimalVertexParallel()
        {
            InitHandlers();

            indexes = new int[threadsCount];

            for (int threadNumber = 0; threadNumber < threadsCount; threadNumber++)
            {
                var job = new JobToDo() { Min = true, PartNumber = threadNumber };
                threadPool.EnqueueTask(job);
            }

            WaitHandle.WaitAll(waitHandlers);
            ResetHandlers();

            var minIndex = indexes[0];
            var min = distances[minIndex];
            for (int index = 1; index < threadsCount; index++)
            {
                var vertex = indexes[index];
                var val = distances[vertex];
                if (val < min)
                {
                    min = val;
                    minIndex = vertex;
                }
            }

            return minIndex;
        }

        private void WorkerFindMinimal(int threadNumber)
        {
            int thread = threadNumber;

            var portion = nodesToVisitList.Count / threadsCount;
            var from = thread * portion;
            var to = thread == threadsCount - 1 ? nodesToVisitList.Count : (thread + 1) * portion;

            var minIndex = nodesToVisitList[from];
            var minValue = distances[minIndex];

            for (int index = from; index < to; index++)
            {
                var vertex = nodesToVisitList[index];
                var value = distances[vertex];
                if (value < minValue)
                {
                    minIndex = vertex;
                    minValue = value;
                }
            }

            indexes[thread] = minIndex;
        }

        private void SingleVisit()
        {
            nodesToVisitList = nodesToVisit.ToList();

            var minimalNode = FindMinVertex();
            nodesToVisit.Remove(minimalNode);
            visitedNodes.Add(minimalNode);

            var edges = graph.GetEdgesFrom(minimalNode);

            var currentDistance = distances[minimalNode];
            Parallel.ForEach(edges, edge => ExploreEdge(edge, currentDistance));

            var newEdges = edges.Where(edge => !visitedNodes.Contains(edge.To) &&
                        !nodesToVisit.Contains(edge.To)).ToList();

            foreach (var edge in newEdges)
                nodesToVisit.Add(edge.To);
        }

        private int FindMinVertex()
        {
            var minIndex = nodesToVisitList[0];
            var min = distances[minIndex];

            for (int index = 0; index < nodesToVisit.Count; index++)
            {
                var vertex = nodesToVisitList[index];
                var distance = distances[vertex];
                if (distance < min)
                {
                    min = distance;
                    minIndex = vertex;
                }
            }

            return minIndex;
        }

        private void ExploreEdge(Edge edge, double currentDistance)
        {
            var newDistance = currentDistance + edge.Distance;
            if (distances[edge.To] > newDistance)
                distances[edge.To] = newDistance;
        }

        private void Initialize(int startVertex)
        {
            visitedNodes.Clear();
            nodesToVisit.Clear();

            distances = new double[graph.VertexesCount];
            for (int index = 0; index < distances.Length; index++)
                distances[index] = int.MaxValue;

            distances[startVertex] = 0;
            nodesToVisit.Add(startVertex);
        }
    }
}
