using System.Collections.Generic;
using System.Diagnostics;

namespace SjUpdater.Utils
{
    public static class timehelper
    {
        public static List<double> dateTimes = new List<double>();

        private static Stopwatch sw;

        static timehelper()
        {
            sw = Stopwatch.StartNew();
        }

        public static void Add()
        {
            dateTimes.Add(sw.Elapsed.TotalSeconds);
        }
    }
}
