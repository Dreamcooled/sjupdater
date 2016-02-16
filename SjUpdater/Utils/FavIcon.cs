using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Amib.Threading;

namespace SjUpdater.Utils
{
    public class FavIcon : PropertyChangedImpl
    {

        private static Dictionary<string, BitmapImage> _dictCache = new Dictionary<string, BitmapImage>();
        private static readonly string cachePath = Path.Combine(Path.GetTempPath(), "sjupdater", "faviconcache");
        private ImageSource _image;
        private string _name;
        private static object lockobj = new object();

        static FavIcon()
        {
            Directory.CreateDirectory(cachePath);
            var files = Directory.GetFiles(cachePath);
            foreach (var file in files)
            {
                var fs = new FileStream(file, FileMode.Open, FileAccess.Read);
                var bitmap = CachedBitmap.BitmapImageFromStream(fs);
                _dictCache.Add(Path.GetFileNameWithoutExtension(file), bitmap);
                fs.Close();
            }
        }

        public FavIcon(string name)
        {
            Name = name;
        }

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name == value) return;
                _name = value;

                var b = GetFromCache(value);
                if (b == null) b = GetFromLetters(value);
                Image = b;

                StaticInstance.ThreadPool.QueueWorkItem(() =>
                {
                    lock(lockobj)
                    {
                        var x = Get(value);
                        if (x != null)
                        {
                            Image = x;
                        }
                    }
                }, true, WorkItemPriority.AboveNormal);
                OnPropertyChanged();
            }
        }

        public ImageSource Image
        {
            get { return _image; }
            set
            {
                if (_image == value) return;
                _image = value;
                OnPropertyChanged();
            }
        }

        private static BitmapImage Get(string value)
        {
            try
            {
                return GetFromCache(value) ?? GetFromUrl(value);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static BitmapImage GetFromLetters(string value)
        {
            var bmp = new Bitmap(48, 48);
            var rectf = new RectangleF(0, 0, 48, 48);
            var g = Graphics.FromImage(bmp);
            //g.Clear(System.Drawing.Color.Gray);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            var format = new StringFormat();
            format.LineAlignment = StringAlignment.Center;
            format.Alignment = StringAlignment.Center;
            var c = ((SolidColorBrush)App.Current.FindResource("LabelTextBrush")).Color; // ye i know, it's hacky but it works
            g.DrawString("" + value.ToUpper().First(), new Font("Tahoma", 40, FontStyle.Bold, GraphicsUnit.Pixel), new SolidBrush(System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B)), rectf, format);
            g.Flush();
            var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            ms.Position = 0;
            return CachedBitmap.BitmapImageFromStream(ms);
        }

        private static BitmapImage GetFromCache(string value)
        {
            value.ToLower();
            foreach (var key in _dictCache.Keys)
            {
                var key2 = key.ToLower();
                if (key2.Contains(value) || value.Contains(key2))
                {
                    return _dictCache[key];
                }
            }
            return null;
        }

        private static string FindUrl(string value)
        {
            if (!value.StartsWith("http"))
            {
                value = "http://" + value;
            }
            var u = new Uri(value);
            if (Uri.CheckHostName(u.DnsSafeHost) == UriHostNameType.Unknown)
            {
                return null;
            }

            var req = HttpWebRequest.CreateHttp(value);
            req.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64)";
            req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            req.AllowAutoRedirect = true;
            var res = req.GetResponse();
            var reader = new StreamReader(res.GetResponseStream());
            var html = reader.ReadToEnd();
            var mts = new Regex("<link\\s+[^>]*", RegexOptions.IgnoreCase).Matches(html);
            foreach (Match mt in mts)
            {
                var m = mt.Value.ToLower();
                if (new Regex("rel\\s*=\\s*['\"][a-z0-9_\\- ]*(icon|shortcut)[a-z0-9_\\- ]*['\"]", RegexOptions.IgnoreCase).Match(m).Success)
                {
                    var murl = new Regex("href\\s*=\\s*['\"]([^'\"]+)['\"]", RegexOptions.IgnoreCase).Match(m);
                    if (murl.Success)
                    {
                        var path = murl.Groups[1].Value;
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
            var url = FindUrl(value);
            if (url == null) return null;
            var url_parts = new Uri(url).DnsSafeHost.Split(new char[] {'.'});
            var key = url_parts[url_parts.Length - 2];

            var ms = new MemoryStream();
            var request = WebRequest.CreateHttp(url);

            var response = request.GetResponse() as HttpWebResponse;
            var responseStream = response.GetResponseStream();
            responseStream.CopyTo(ms);
            responseStream.Close();
            response.Close();

            ms.Position = 0;
            var f = new FileStream(Path.Combine(cachePath, key + url.Substring(url.LastIndexOf('.'))), FileMode.Create, FileAccess.Write);
            ms.CopyTo(f);

            f.Close();
            ms.Position = 0;
            var bmap = CachedBitmap.BitmapImageFromStream(ms);
            _dictCache.Add(key, bmap);
            return bmap;
        }
    }
}
