using System;

namespace Lab2.Data
{
    [Serializable]
    public class JobDone
    {
        public JobDone(double[] arr)
        {
            this.array = arr;
        }

        public double[] array;
    }
}
