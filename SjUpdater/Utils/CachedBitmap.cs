using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
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
        private readonly Dispatcher dispatcher;
        private String _url;
        private ImageSource _source;
        private IWorkItemResult _workItem;

        public CachedBitmap()
        {
            dispatcher = Dispatcher.CurrentDispatcher;
            _url = "";
            _source = null;
            _workItem = null;
        }

        public CachedBitmap(string url) :this()
        {
            Url = url;
        }

       /* private static void PropertyChangedCallback(DependencyObject dependencyObject,
                                                    DependencyPropertyChangedEventArgs
                                                        dependencyPropertyChangedEventArgs)*/
        private void load_image(String url)
        {
            if (_workItem != null && !_workItem.IsCompleted) 
                return;
            _workItem = StaticInstance.ThreadPool.QueueWorkItem(() =>
                                                                {
                                                                    // var instance = dependencyObject as CachedBitmap;
                                                                    //SetLoading(true, instance);


                                                                    HashAlgorithm hashAlg = new SHA512CryptoServiceProvider();

                                                                    Crc64 crc64 = new Crc64(Crc64Iso.Iso3309Polynomial);

                                                                    string urlcrc64 =
                                                                        BitConverter.ToString(crc64.ComputeHash(Encoding.ASCII.GetBytes(url)))
                                                                                    .Replace("-", "").ToLower();

                                                                    string cachePath = Path.Combine(Path.GetTempPath(), "sjupdater", "imagechache");
                                                                    string filepath = Path.Combine(cachePath, urlcrc64.ToLower() + ".imagecache");

                                                                    Action downloadAndSetAndCache = () =>
                                                                                                    {
                                                                                                        using (MemoryStream ms = DownloadData(url))
                                                                                                        {
                                                                                                            if (ms == null) return;

                                                                                                            BitmapSource bitmapSource = BitmapImageFromStream(ms);
                                                                                                            dispatcher.Invoke(() =>
                                                                                                                              {

                                                                                                                                  ImageSource = bitmapSource;
                                                                                                                              });

                                                                                                            CacheData(filepath, ms, hashAlg);
                                                                                                        }
                                                                                                    };

                                                                    if (File.Exists(filepath))
                                                                    {
                                                                        byte[] cacheHash;

                                                                        MemoryStream ms = GetCachedData(filepath, hashAlg, out cacheHash);
                                                                        BitmapImage image = BitmapImageFromStream(ms);
                                                                        //  SetLoading(false, instance);

                                                                        dispatcher.Invoke(() =>
                                                                                          {
                                                                                              ImageSource = image;
                                                                                          });

                                                                        MemoryStream data = DownloadData(url);
                                                                        if (data == null)
                                                                            return;

                                                                        byte[] downloadHash = new byte[hashAlg.HashSize / 8];
                                                                        downloadHash = hashAlg.ComputeHash(data);

                                                                        if (!cacheHash.Memcmp(downloadHash))
                                                                        {
                                                                            // SetLoading(true, instance);
                                                                            downloadAndSetAndCache();
                                                                            //  SetLoading(false, instance);
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        // SetLoading(true, instance);
                                                                        downloadAndSetAndCache();
                                                                        //  SetLoading(false, instance);
                                                                    }
                                                                }, true, ThreadPriority.Lowest);
        }

       /* private static void SetLoading(bool value, CachedBitmap instance)
        {
            instance.dispatcher.Invoke(() =>
            {
                instance.IsLoading = value;
            });
        }*/

        private static void CacheData(string file, Stream stream, HashAlgorithm hashAlgorithm)
        {
            stream.Position = 0;
            byte[] dataHash = hashAlgorithm.ComputeHash(stream);

            string dir = Path.GetDirectoryName(file);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            try
            {
                using (FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(dataHash, 0, dataHash.Length);

                    stream.Position = 0;
                    stream.CopyTo(fs);
                }
            }
            catch
            {
                // Don't worry if we can't write, doesn't affect application function ~Calvin 11-Feb-2016
            }
        }

        private static MemoryStream GetCachedData(string file, HashAlgorithm hashAlgorithm, out byte[] cacheHash)
        {
                cacheHash = new byte[hashAlgorithm.HashSize / 8];

            MemoryStream ms = new MemoryStream();

            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                fs.Read(cacheHash, 0, cacheHash.Length);

                fs.CopyTo(ms);
            }
            ms.Position = 0;
            return ms;
        }

        static MemoryStream DownloadData(string url)
        {
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
                BitmapImage image = new BitmapImage();
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

        [NotMapped]
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

        public String Url
        {
            get
            {
                return _url;
            }
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

        /*public static readonly DependencyProperty UrlProperty =
            DependencyProperty.Register("Url", typeof (String), typeof (CachedBitmap),
                new PropertyMetadata(default(String), PropertyChangedCallback));

        public string Url
        {
            get { return (String)GetValue(UrlProperty); }
            internal set { SetValue(UrlProperty, value); }
        }

        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register("ImageSource", typeof (ImageSource), typeof (CachedBitmap),
                new PropertyMetadata(default(ImageSource)));

        [NotMapped]
        [XmlIgnore]
        public ImageSource ImageSource
        {
            get { return (ImageSource)GetValue(ImageSourceProperty); }
            private set { SetValue(ImageSourceProperty, value); }
        }

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(CachedBitmap),
                new PropertyMetadata(default(bool)));

        [NotMapped]
        [XmlIgnore]
        public bool IsLoading
        {
            get { return (bool)GetValue(IsLoadingProperty); }
            private set { SetValue(IsLoadingProperty, value); }
        }*/
    }
}
