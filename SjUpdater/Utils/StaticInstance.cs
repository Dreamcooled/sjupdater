namespace SjUpdater.Utils
{
    public static class StaticInstance
    {
        public static ThreadPool ThreadPool { get; }

        static StaticInstance()
        {
            ThreadPool = new ThreadPool();
        }
    }
}
