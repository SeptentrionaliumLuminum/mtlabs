using System;
using System.Collections.Generic;

namespace Lab3.Services
{
    internal enum PivotType
    {
        Middle,
        Median
    }

    internal static class QuickSort
    {
        public static PivotType PivotType = PivotType.Median;

        internal static void Sort<T>(IList<T> array, int low, int high) where T : IComparable<T>
        {
            if (low >= high)
                return;

            var pivot = Partition(array, low, high);
            Sort(array, low, pivot - 1);
            Sort(array, pivot + 1, high);
        }

        internal static int Partition<T>(IList<T> array, int low, int high) where T : IComparable<T>
        {
            switch (PivotType)
            {
                case PivotType.Median:
                    return PartitionUsingMedian(array, low, high);
                case PivotType.Middle:
                default:
                    return PartitionUsingMiddle(array, low, high);
            }               
        }

        internal static int PartitionUsingMedian<T>(IList<T> array, int low, int high) where T : IComparable<T>
        {
            var middleIndex = (low + high) / 2;

            var medianIndex = MedianIndex(array[low], low, array[middleIndex], middleIndex, array[high], high);
            Swap(array, middleIndex, medianIndex);

            return PartitionUsingMiddle(array, low, high);
        }

        private static int PartitionUsingMiddle<T>(IList<T> array, int low, int high) where T : IComparable<T>
        {
            var pivotIndex = (low + high) / 2;
            var pivotElement = array[pivotIndex];

            Swap(array, low, pivotIndex);

            var leftIndex = low;

            for (var index = low + 1; index <= high; index++)
            {
                var isMore = array[index].CompareTo(pivotElement) >= 0;
                if (isMore)
                    continue;

                leftIndex++;
                Swap(array, index, leftIndex);
            }

            Swap(array, low, leftIndex);

            return leftIndex;
        }

        private static int MedianIndex<T>(T a, int aIndex, T b, int bIndex, T c, int cIndex) where T : IComparable<T>
        {
            if (b.CompareTo(a) < 0)
            {
                if (a.CompareTo(c) < 0)
                {
                    //b a c
                    return aIndex;
                }
                else
                {
                    if (c.CompareTo(b) < 0)
                        //c b a
                        return bIndex;
                    else
                        //b c a
                        return cIndex;
                }
            }
            if (b.CompareTo(c) < 0)
            {
                //a b c
                return bIndex;
            }
            if (c.CompareTo(a) < 0)
            {
                //c a b
                return aIndex;
            }
            else
            {
                //a c b
                return cIndex;
            }
        }

        public static void Swap<T>(IList<T> array, int firstIndex, int secondIndex)
        {
            var temp = array[firstIndex];
            array[firstIndex] = array[secondIndex];
            array[secondIndex] = temp;
        }
    }
}
