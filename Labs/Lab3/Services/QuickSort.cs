using System;
using System.Collections.Generic;

namespace Lab3.Services
{
    internal static class QuickSort
    {
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

        internal static void Swap<T>(IList<T> array, int firstIndex, int secondIndex)
        {
            var temp = array[firstIndex];
            array[firstIndex] = array[secondIndex];
            array[secondIndex] = temp;
        }
    }
}
