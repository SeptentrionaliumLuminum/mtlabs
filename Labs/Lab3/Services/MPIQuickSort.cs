using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using MPI;

namespace Lab3.Services
{
    internal class MPIQuickSort
    {
        #region Nested classes

        [Serializable]
        private class DataPackage
        {
            public DataPackage(int[] array, int low, int high, int deep)
            {
                Array = array;
                Low = low;
                High = high;
                Deep = deep;
            }

            public int[] Array;
            public int Low;
            public int High;
            public int Deep;
        }

        #endregion

        private const int minimalSize = 1024;

        private const int doWorkTag = 28;
        private const int receiveResultTag = 56;
        private const int terminateTag = 110;

        private readonly Intracommunicator communicator;

        private IList<int> waitingList = new List<int>();
        private int deep = 0;

        public MPIQuickSort(Intracommunicator intracommunicator)
        {
            communicator = intracommunicator ?? throw new ArgumentNullException(nameof(intracommunicator));
        }

        public IList<int> ParallelSort(IList<int> array, int low, int high)
        {
            ParallelQuickSort(array, low, high);
            CollectPackages(array);

            return array;
        }

        public void WorkerLoop()
        {
            var deepLog = Math.Log(communicator.Size, 2);
            deep = (int)Math.Floor(deepLog);
            if (communicator.Rank >= Math.Pow(2, deep))
                return;

            var parentRank = GetParentRank();

            Console.WriteLine($"Worker {communicator.Rank} processing requests from {parentRank}");

            var jobRequest = communicator.ImmediateReceive<DataPackage>(parentRank, doWorkTag);
            jobRequest.Wait();

            var package = jobRequest.GetValue() as DataPackage;

            var arrayAsList = package.Array.ToList();
            deep = package.Deep;
            ParallelQuickSort(arrayAsList, 0, arrayAsList.Count - 1);
            CollectPackages(arrayAsList);

            var responsePackage = new DataPackage(arrayAsList.ToArray(), package.Low, package.High, deep);
            communicator.ImmediateSend(responsePackage, parentRank, receiveResultTag);
        }

        private int GetParentRank()
        {
            //TODO: Replace with formula
            if (communicator.Rank == 1)
                return 0;
            if (communicator.Rank == 2)
                return 1;
            if (communicator.Rank == 3)
                return 0;
            if (communicator.Rank == 5)
                return 2;
            if (communicator.Rank == 6)
                return 1;
            if (communicator.Rank == 4)
                return 3;
            if (communicator.Rank == 7)
                return 0;
            if (communicator.Rank >= 8)
                return 15 - communicator.Rank;

            throw new ArgumentOutOfRangeException(nameof(communicator.Rank));
        }

        private void CollectPackages(IList<int> array)
        {
            var requests = waitingList.Select(rank => communicator.ImmediateReceive<DataPackage>(rank, receiveResultTag)).ToList();

            while (requests.Count > 0)
            {
                var completed = requests.Where(r => r.Test() != null);
                requests = requests.Except(completed).ToList();

                foreach (var request in completed)
                {
                    var dataPackage = request.GetValue() as DataPackage;
                    Transfer(dataPackage, array);
                }
            }
        }

        private void Transfer(DataPackage package, IList<int> array)
        {
            for (int index = package.Low; index <= package.High; index++)
                array[index] = package.Array[index - package.Low];
        }

        private void ParallelQuickSort(IList<int> array, int low, int high)
        {
            if (low >= high)
                return;

            deep++;

            var pivot = QuickSort.Partition(array, low, high);

            if (CanUseMultithreading(array, low, high))
            {
                SendToWorker(array, low, pivot - 1);
                ParallelQuickSort(array, pivot + 1, high);
            }
            else
            {
                QuickSort.Sort(array, low, pivot - 1);
                QuickSort.Sort(array, pivot + 1, high);
            }
        }

        private int GetTargetCommunicatorRank()
        {
            return ((int)Math.Pow(2, deep) - 1) - communicator.Rank;
        }

        private bool CanUseMultithreading(IList<int> array, int low, int high)
        {
            if (high - low < minimalSize)
                return false;

            if (GetTargetCommunicatorRank() >= communicator.Size)
                return false;

            return true;
        }

        private void SendToWorker(IList<int> array, int low, int high)
        {
            var targetCommunicator = GetTargetCommunicatorRank();

            var subArray = Cut(array, low, high);
            var dataPackage = new DataPackage(subArray, low, high, deep);

            var request = communicator.ImmediateSend(dataPackage, targetCommunicator, doWorkTag);
            request.Test();

            Console.WriteLine($"{communicator.Rank} send package to {targetCommunicator}");

            waitingList.Add(targetCommunicator);
        }

        private static int[] Cut(IList<int> array, int from, int to)
        {
            var resultArray = new int[to - from + 1];
            for (int index = from; index <= to; index++)
                resultArray[index - from] = array[index];

            return resultArray;
        }
    }
}
