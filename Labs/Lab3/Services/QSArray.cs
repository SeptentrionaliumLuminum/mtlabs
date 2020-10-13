using System;
using System.Collections.Generic;

namespace Lab3.Services
{
    internal class QSArray
    {
        private List<int> array;

        public QSArray(List<int> array)
        {
            this.array = array;
        }

        public QSArray(int[] array)
        {
            this.array = new List<int>(array);
        }

        public int[] GetContent() => array.ToArray();

        public IList<int> List => array;

        internal QSArray GetPart(int part, int totalParts)
        {
            int partSize = array.Count / totalParts;
            int oddSize = array.Count % totalParts;
            int begin = part * partSize;
            begin += Math.Min(part, oddSize);

            var length = part < oddSize ? partSize + 1 : partSize;

            return new QSArray(array.GetRange(begin, length));
        }

        internal int GetPivot()
        {
            //TODO: Different pivot types?
            return array[0];
        }

        internal void Partition(int pivot, out QSArray low, out QSArray high)
        {
            int i = -1;
            int j = array.Count;

            while (true)
            {
                do
                {
                    i++;
                }
                while (array[i] < pivot);
                do
                {
                    j--;
                }
                while (array[j] > pivot);
                if (i >= j) break;
                QuickSort.Swap(array, i, j);
            }
            j++;

            low = new QSArray(array.GetRange(0, j));
            high = new QSArray(array.GetRange(j, array.Count - j));
        }

        public static QSArray Merge(QSArray low, QSArray high)
        {
            var array = new List<int>();
            array.AddRange(low.GetContent());
            array.AddRange(high.GetContent());

            return new QSArray(array);
        }
    }
}
