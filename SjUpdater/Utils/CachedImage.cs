using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Amib.Threading;

namespace SjUpdater.Utils
{
    public class CachedImage : PropertyChangedImpl
    {
        private static readonly SmartThreadPool _threadPool = new SmartThreadPool(new STPStartInfo
        {
            AreThreadsBackground = true,
            MaxWorkerThreads = 15
        });

        private readonly Dispatcher _dispatcher;
        private string _cacheFile;

        private string _url;
        private ImageSource _imageSource;

        /// <summary>
        /// Url of remote source
        /// </summary>
        public string Url
        {
            get { return _url; }
            private set
            {
                _url = value;
                _cacheFile = GetCacheFilePath(_url);
            }
        }

        /// <summary>
        /// Seconds since creation of cache until it gets invalidated and refreshed
        /// </summary>
        public long Validity { get; set; }

        public ImageSource ImageSource
        {
            get
            {
                if (_imageSource == null)
                    _threadPool.QueueWorkItem(LoadBitmap);

                return _imageSource;
            }
            set
            {
                if (Equals(value, _imageSource)) return;
                _imageSource = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// A Image from an HTTP source using a local cache
        /// </summary>
        /// <param name="url">Url to Image</param>
        /// <param name="preLoad">if true, image gets loaded at initialization, otherwise when accessing property ImageSource</param>
        /// <param name="validity">time in seconds until local cache gets updated by remote source. Default is 1 week</param>
        public CachedImage(string url, bool preLoad = false, long validity = 60 * 60 * 24 * 7)
        {
            _dispatcher = Dispatcher.CurrentDispatcher;

            Url = url;
            Validity = validity;

            if (preLoad)
                _threadPool.QueueWorkItem(LoadBitmap);
        }

        private void LoadBitmap()
        {
            var loadedFromDisk = false;

            if (Validity > 0)
            {
                try
                {
                    if (File.Exists(_cacheFile))
                    {
                        using (var fileStream = new FileStream(_cacheFile, FileMode.Open, FileAccess.Read))
                        {
                            var image = ImageSourceFromStream(fileStream);

                            _dispatcher.Invoke(() =>
                            {
                                ImageSource = image;
                            });
                            loadedFromDisk = true;
                        }
                    }
                }
                catch
                {
                    // ignored
                }
            }

            try
            {
                if (!loadedFromDisk || File.GetLastWriteTime(_cacheFile).AddSeconds(Validity) < DateTime.Now)
                {
                    var memoryStream = DownloadData(Url);

                    var image = ImageSourceFromStream(memoryStream);
                    _dispatcher.Invoke(() => { ImageSource = image; });

                    if (Validity > 0)
                    {
                        if (!Directory.Exists(Path.GetDirectoryName(_cacheFile)))
                            Directory.CreateDirectory(Path.GetDirectoryName(_cacheFile));

                        using (var fileStream = new FileStream(_cacheFile, FileMode.Create, FileAccess.Write))
                        {
                            memoryStream.Seek(0, SeekOrigin.Begin);
                            memoryStream.CopyTo(fileStream);
                        }
                    }
                }
            }
            catch
            {
                //ignored
            }
        }

        private static MemoryStream DownloadData(string url)
        {
            var memoryStream = new MemoryStream();
            var request = WebRequest.CreateHttp(url);

            using (var response = request.GetResponse())
            {
                using (var responseStream = response.GetResponseStream())
                {
                    responseStream?.CopyTo(memoryStream);
                }
            }

            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }

        public static ImageSource ImageSourceFromStream(Stream stream)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = stream;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();

            return image;
        }

        private static string GetCacheFilePath(string url)
        {
            var md5 = new MD5CryptoServiceProvider();
            var hash = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(url))).Replace("-", "").ToLower();

            var cacheDirectory = Path.Combine(Path.GetTempPath(), "sjupdater", "imagechache");
            var cacheFile = Path.Combine(cacheDirectory, hash.ToLower() + ".imagecache");

            return cacheFile;
        }
    }
}
