using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SjUpdater.Utils
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Checks whether 2 byte arrays equals
        /// </summary>
        /// <param name="array"></param>
        /// <param name="array2"></param>
        /// <returns></returns>
        public static bool Memcmp(this byte[] array, byte[] array2)
        {
            if (array.Length != array2.Length)
                return false;

            return Native.memcmp(array, array2, array.Length) == 0;
        }

        public static Comparer<T> CreateMultiple<T>(params Comparer<T>[] comparers  )
        {
            return Comparer<T>.Create(delegate(T x, T y)
            {
                foreach (var comparer in comparers)
                {
                    int i = comparer.Compare(x, y);
                    if (i != 0) return i;
                }
                return 0;
            });
        }

        public static void Sort<TSource>(this ObservableCollection<TSource> source, Comparer<TSource> comparer, bool desc = false)
        {
            if (source == null) return;

            for (int i = source.Count - 1; i >= 0; i--)
            {
                for (int j = 1; j <= i; j++)
                {
                    TSource o1 = source[j - 1];
                    TSource o2 = source[j];
                    int comparison = comparer.Compare(o1, o2);
                    if (desc && comparison < 0)
                        source.Move(j, j - 1);
                    else if (!desc && comparison > 0)
                        source.Move(j - 1, j);
                }
            }
        }
    }
}
