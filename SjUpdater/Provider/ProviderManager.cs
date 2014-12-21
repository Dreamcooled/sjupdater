using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SjUpdater.Provider
{
    public static class ProviderManager
    {
        private static readonly IProvider Provider;

        static ProviderManager()
        {
            Provider = new TMDb();
        }

        public static IProvider GetProvider()
        {
            return Provider;
        }

    }
}
