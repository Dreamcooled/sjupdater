using Amib.Threading;

namespace SjUpdater.Utils
{
    public static class StaticInstance
    {
        public static SmartThreadPool SmartThreadPool = new SmartThreadPool();

        static StaticInstance()
        {
            SmartThreadPool = new SmartThreadPool(new STPStartInfo()
            {
                AreThreadsBackground = true,
                UseCallerCallContext = true
            });
        }
    }
}
