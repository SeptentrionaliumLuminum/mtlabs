using System;
using System.Collections.Generic;
using System.Threading;

namespace Lab4
{
    /// <summary>
    /// For Threads re-usage
    /// </summary>
    public class CustomThreadPool<T> : IDisposable where T : class
    {
        readonly object _locker = new object();
        readonly List<Thread> _workers;
        readonly Queue<T> _taskQueue = new Queue<T>();
        readonly Action<T> _dequeueAction;

        public CustomThreadPool(int workerCount, Action<T> dequeueAction)
        {
            _dequeueAction = dequeueAction;
            _workers = new List<Thread>(workerCount);

            // Create and start a separate thread for each worker
            for (int i = 0; i < workerCount; i++)
            {
                Thread t = new Thread(Consume) { IsBackground = true, Priority = ThreadPriority.Highest };
                _workers.Add(t);
                t.Start();
            }
        }

        public void EnqueueTask(T task)
        {
            lock (_locker)
            {
                _taskQueue.Enqueue(task);
                Monitor.PulseAll(_locker);
            }
        }

        void Consume()
        {
            while (true)
            {
                T item;
                lock (_locker)
                {
                    while (_taskQueue.Count == 0) Monitor.Wait(_locker);
                    item = _taskQueue.Dequeue();
                }
                if (item == null) return;

                // run actual method
                _dequeueAction(item);
            }
        }

        public void Dispose()
        {
            _workers.ForEach(thread => EnqueueTask(null));
            _workers.ForEach(thread => thread.Join());
        }
    }
}
