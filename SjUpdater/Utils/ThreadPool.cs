using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amib.Threading;
using Action = Amib.Threading.Action;

namespace SjUpdater.Utils
{
    public class ThreadPool
    {
        private volatile ConcurrentBag<SmartThreadPool> threadPools = new ConcurrentBag<SmartThreadPool>();
        private int maxThreads;

        public int MaxThreads
        {
            get
            {
                return maxThreads;
            }
            set
            {
                maxThreads = value;
                foreach (var threadPool in threadPools)
                {
                    threadPool.MaxThreads = maxThreads;
                }
            }
        }

        public ThreadPool(int maxWorkerThreads = 10)
        {
            this.maxThreads = maxWorkerThreads;
        }

        private SmartThreadPool getPool(bool background, ThreadPriority priority, WorkItemPriority itemPriority)
        {
            var pool = threadPools.FirstOrDefault(p => p.STPStartInfo.ThreadPriority == priority && p.STPStartInfo.AreThreadsBackground == background);

            if (pool == null)
            {
                pool = new SmartThreadPool(new STPStartInfo
                                           {
                                               AreThreadsBackground = background,
                                               ThreadPriority = priority,
                                               MaxWorkerThreads = maxThreads
                                           });
                pool.Start();
                threadPools.Add(pool);
            }

            return pool;
        }

        public IWorkItemResult QueueWorkItem(Action action, bool background = true, ThreadPriority priority = ThreadPriority.Normal, WorkItemPriority itemPriority = WorkItemPriority.Normal)
        {
            var pool = getPool(background, priority, itemPriority);
            return pool.QueueWorkItem(action, itemPriority);
        }

        public IWorkItemResult QueueWorkItem<T>(Action<T> action, T item, bool background = true, ThreadPriority priority = ThreadPriority.Normal, WorkItemPriority itemPriority = WorkItemPriority.Normal)
        {
            var pool = getPool(background, priority, itemPriority);
            return pool.QueueWorkItem(action, item, itemPriority);
        }
    }
}
