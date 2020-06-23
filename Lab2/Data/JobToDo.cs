using System;

namespace Lab2.Data
{
    [Serializable]
    public class JobToDo
    {
        public JobToDo(int start, int end, double[] initial)
        {
            this.start = start;
            this.end = end;
            this.initial = initial;
        }

        public JobToDo(bool finish)
        {
            this.finish = finish;
        }

        public int start;
        public int end;
        public double[] initial;

        public bool finish = false;
    }
}
