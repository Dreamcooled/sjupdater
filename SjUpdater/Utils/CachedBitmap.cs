using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Serialization;
using Amib.Threading;
using SjUpdater.XML;
using Action = System.Action;

namespace SjUpdater.Utils
{
    [XmlIgnoreBaseType]
    public class CachedBitmap : PropertyChangedImpl
    {
        private readonly Dispatcher _dispatcher;
        private string _url;
        private ImageSource _source;
        private IWorkItemResult _workItem;

        public CachedBitmap()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _url = "";
            _source = null;
            _workItem = null;
        }

        public CachedBitmap(string url) : this()
        {
            Url = url;
        }

        private void load_image(string url)
        {
            if (_workItem != null && !_workItem.IsCompleted)
                return;

            _workItem = StaticInstance.ThreadPool.QueueWorkItem(() =>
            {
                HashAlgorithm hashAlg = new SHA512CryptoServiceProvider();

                var crc64 = new Crc64(Crc64Iso.Iso3309Polynomial);

                var urlcrc64 =
                    BitConverter.ToString(crc64.ComputeHash(Encoding.ASCII.GetBytes(url)))
                        .Replace("-", "").ToLower();

                var cachePath = Path.Combine(Path.GetTempPath(), "sjupdater", "imagechache");
                var filepath = Path.Combine(cachePath, urlcrc64.ToLower() + ".imagecache");

                Action downloadAndSetAndCache = () =>
                {
                    using (var ms = DownloadData(url))
                    {
                        if (ms == null) return;

                        BitmapSource bitmapSource = BitmapImageFromStream(ms);
                        _dispatcher.Invoke(() => { ImageSource = bitmapSource; });

                        CacheData(filepath, ms, hashAlg);
                    }
                };

                if (File.Exists(filepath))
                {
                    byte[] cacheHash;

                    var ms = GetCachedData(filepath, hashAlg, out cacheHash);
                    var image = BitmapImageFromStream(ms);

                    _dispatcher.Invoke(() => ImageSource = image);

                    var data = DownloadData(url);
                    if (data == null)
                        return;

                    var downloadHash = hashAlg.ComputeHash(data);

                    if (!cacheHash.Memcmp(downloadHash))
                    {
                        downloadAndSetAndCache();
                    }
                }
                else
                {
                    downloadAndSetAndCache();
                }
            }, true, WorkItemPriority.AboveNormal);
        }

        private static void CacheData(string file, Stream stream, HashAlgorithm hashAlgorithm)
        {
            stream.Position = 0;
            var dataHash = hashAlgorithm.ComputeHash(stream);

            var dir = Path.GetDirectoryName(file);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using (var fs = new FileStream(file, FileMode.Create, FileAccess.Write))
            {
                fs.Write(dataHash, 0, dataHash.Length);

                stream.Position = 0;
                stream.CopyTo(fs);
            }
        }

        private static MemoryStream GetCachedData(string file, HashAlgorithm hashAlgorithm, out byte[] cacheHash)
        {
            cacheHash = new byte[hashAlgorithm.HashSize / 8];

            var ms = new MemoryStream();

            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                fs.Read(cacheHash, 0, cacheHash.Length);

                fs.CopyTo(ms);
            }
            ms.Position = 0;
            return ms;
        }

        private static MemoryStream DownloadData(string url)
        {
            try
            {
                var ms = new MemoryStream();
                var request = WebRequest.CreateHttp(url);

                var response = request.GetResponse() as HttpWebResponse;
                var responseStream = response.GetResponseStream();
                responseStream.CopyTo(ms);
                responseStream.Close();
                response.Close();

                ms.Position = 0;

                return ms;
            }
            catch
            {
                return null;
            }
        }

        public static BitmapImage BitmapImageFromStream(Stream stream, bool freeze = true)
        {
            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                if (freeze)
                    image.Freeze();

                return image;
            }
            catch (Exception)
            {


            }
            return null;
        }

        [XmlIgnore]
        public ImageSource ImageSource
        {
            get
            {
                if (_source == null && !string.IsNullOrWhiteSpace(_url))
                {
                    load_image(_url); //request the image (async)
                    return null; //we need to request it first
                }
                return _source;
            }
            private set
            {
                _source = value;
                OnPropertyChanged();
            }
        }

        public string Url
        {
            get { return _url; }
            set
            {
                if (_url == value)
                    return;
                _url = value;
                _source = null; //force refetch on next request
                OnPropertyChanged();
                OnPropertyChanged("ImageSource"); //force refetch of ImageSource
            }
        }
    }
}
