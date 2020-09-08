using System;
using System.Diagnostics;

namespace Lab2
{
    internal class PerformanceCounter : IDisposable
    {
        private Stopwatch stopwatch;
        private string message;

        private DateTime startDate;

        public PerformanceCounter(string message)
        {
            this.message = message;

            startDate = DateTime.Now;
            stopwatch = new Stopwatch();
            stopwatch.Start();
        }

        public void Dispose()
        {
            stopwatch.Stop();
            Console.WriteLine($"{message} {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}
