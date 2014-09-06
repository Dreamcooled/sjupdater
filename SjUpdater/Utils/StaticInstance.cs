using Amib.Threading;

namespace SjUpdater.Utils
{
    public static class StaticInstance
    {
        private static readonly ThreadPool threadPool;
        public static ThreadPool ThreadPool { get { return threadPool; } }

        static StaticInstance()
        {
            threadPool = new ThreadPool();
        }
    }
}
