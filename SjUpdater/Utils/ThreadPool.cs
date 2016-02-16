using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Amib.Threading;
using Action = Amib.Threading.Action;

namespace SjUpdater.Utils
{
    public class ThreadPool
    {
        private volatile ConcurrentBag<SmartThreadPool> _threadPools = new ConcurrentBag<SmartThreadPool>();
        private int _maxThreads;

        public int MaxThreads
        {
            get { return _maxThreads; }
            set
            {
                _maxThreads = value;
                foreach (var threadPool in _threadPools)
                {
                    threadPool.MaxThreads = _maxThreads;
                }
            }
        }

        public ThreadPool(int maxWorkerThreads = 10)
        {
            _maxThreads = maxWorkerThreads;
        }

        private SmartThreadPool getPool(bool background, ThreadPriority priority, WorkItemPriority itemPriority)
        {
            var pool = _threadPools.FirstOrDefault(p => p.STPStartInfo.ThreadPriority == priority && p.STPStartInfo.WorkItemPriority == itemPriority && p.STPStartInfo.AreThreadsBackground == background);

            if (pool == null)
            {
                pool = new SmartThreadPool(new STPStartInfo
                {
                    AreThreadsBackground = background,
                    ThreadPriority = priority,
                    WorkItemPriority = itemPriority,
                    MaxWorkerThreads = _maxThreads
                });
                pool.Start();
                _threadPools.Add(pool);
            }

            return pool;
        }

        public IWorkItemResult QueueWorkItem(Action action, bool background = true, WorkItemPriority itemPriority = WorkItemPriority.Normal, ThreadPriority priority = ThreadPriority.Normal)
        {
            var pool = getPool(background, priority, itemPriority);
            return pool.QueueWorkItem(action, itemPriority);
        }

        public IWorkItemResult QueueWorkItem<T>(Action<T> action, T item, bool background = true, WorkItemPriority itemPriority = WorkItemPriority.Normal, ThreadPriority priority = ThreadPriority.Normal)
        {
            var pool = getPool(background, priority, itemPriority);
            return pool.QueueWorkItem(action, item, itemPriority);
        }
    }
}
