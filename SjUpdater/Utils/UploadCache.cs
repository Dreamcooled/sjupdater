using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SjUpdater.Model;

namespace SjUpdater.Utils
{
    public class UploadCache 
    {
        private readonly ConcurrentDictionary<int, UploadData> _uploadCache = new ConcurrentDictionary<int, UploadData>();
        public UploadData GetUniqueUploadData(UploadData u)
        {
            if (u == null) return null;
            int k = u.GetHashCode();
            UploadData v;
            if (_uploadCache.TryGetValue(k, out v) && v.Equals(u))
            {
                return v;
            }
            _uploadCache.TryAdd(k, u);
            return u;
        }


    }
}
