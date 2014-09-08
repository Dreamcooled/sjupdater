using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace SjUpdater.Utils
{
    public class FavIcon
    {

        private static  Dictionary<String, BitmapImage> _dictCache = new Dictionary<string, BitmapImage>();
        private readonly static string cachePath = Path.Combine(Path.GetTempPath(), "sjupdater", "faviconcache");
        static FavIcon()
        {
            
            Directory.CreateDirectory(cachePath);
            string[] files = Directory.GetFiles(cachePath);
            foreach (var file in files)
            {
                FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read);
                var bitmap = CachedBitmap.BitmapImageFromStream(fs);
                _dictCache.Add(Path.GetFileNameWithoutExtension(file),bitmap);
                fs.Close();
            }


        }

        public static ImageSource Get(String value)
        {
            try
            {
                var b = GetFromCache(value);
                if (b == null) b = GetFromUrl(value);
                //todo: draw two letters
                return b;
            }
            catch (Exception)
            {

                return null; 
            }

        }


     



        private static BitmapImage GetFromCache(string value)
        {
            value.ToLower();
            foreach (var key in _dictCache.Keys)
            {
                String key2 = key.ToLower();
                if (key2.Contains(value) ||value.Contains(key2))
                {
                    return _dictCache[key];
                }
            }
            return null;
        }





        static String FindUrl(string value)
        {
            if (!value.StartsWith("http"))
            {
                value = "http://" + value;
            }
            Uri u = new Uri(value);
            if (Uri.CheckHostName(u.DnsSafeHost) == UriHostNameType.Unknown)
            {
                return null;
            }


            HttpWebRequest req = HttpWebRequest.CreateHttp(value);
            req.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64)";
            req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            var res = req.GetResponse();
            StreamReader reader = new StreamReader(res.GetResponseStream());
            String html = reader.ReadToEnd();
            MatchCollection mts = new Regex("<link\\s+[^>]*", RegexOptions.IgnoreCase).Matches(html);
            foreach (Match mt in mts)
            {
                String m = mt.Value.ToLower();
                if (new Regex("rel\\s*=\\s*['\"][a-z0-9_\\- ]*(icon|shortcut)[a-z0-9_\\- ]*['\"]", RegexOptions.IgnoreCase).Match(m).Success)
                {
                    Match murl = new Regex("href\\s*=\\s*['\"]([^'\"]+)['\"]", RegexOptions.IgnoreCase).Match(m);
                    if (murl.Success)
                    {
                        String path = murl.Groups[1].Value;
                        if (!path.StartsWith("http"))
                        {
                            path = res.ResponseUri + "/" + path;
                        }
                        return path;
                    }
                }
            }
            return null;
        }

        private static BitmapImage GetFromUrl(string value)
        {
            String url = FindUrl(value);
            if (url == null) return null;
            String[] url_parts = new Uri(url).DnsSafeHost.Split(new char[] {'.'});
            String key = url_parts[url_parts.Length - 2];
            try
            {
                MemoryStream ms = new MemoryStream();
                HttpWebRequest request = WebRequest.CreateHttp(url);

                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                Stream responseStream = response.GetResponseStream();
                responseStream.CopyTo(ms);
                responseStream.Close();
                response.Close();

                ms.Position = 0;
                FileStream f = new FileStream(Path.Combine(cachePath,key+url.Substring(url.LastIndexOf('.'))),FileMode.Create,FileAccess.Write);
                ms.CopyTo(f);
               
                f.Close();
                ms.Position = 0;
                var bmap =  CachedBitmap.BitmapImageFromStream(ms);
                _dictCache.Add(key, bmap);
                return bmap;

            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
