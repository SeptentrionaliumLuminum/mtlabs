using System;
using System.Diagnostics;

namespace Common
{
    public class PerformanceCounter : IDisposable
    {
        private readonly Stopwatch stopwatch;
        private readonly string message;

        public PerformanceCounter(string message)
        {
            this.message = message ?? throw new ArgumentNullException(nameof(message));

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
