using System;
using System.Diagnostics;

namespace Lab2
{
    class Performance : IDisposable
    {
        private Stopwatch stopwatch;
        private string message;

        public Performance(string msg)
        {
            message = msg;
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
