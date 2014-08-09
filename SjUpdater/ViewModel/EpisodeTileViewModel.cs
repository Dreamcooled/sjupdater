using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Threading;
using MahApps.Metro.Converters;
using SjUpdater.Model;
using SjUpdater.Utils;

namespace SjUpdater.ViewModel
{
    public class EpisodeTileViewModel : PropertyChangedImpl
    {
        private readonly FavEpisodeData _favEpisodeData;
        private readonly EpisodeViewModel _vm;
        private CachedBitmap _thumbnail;
        private readonly Dispatcher _dispatcher;

        public EpisodeTileViewModel(FavEpisodeData favEpisodeData)
        {
            _favEpisodeData = favEpisodeData;
            _vm = new EpisodeViewModel(favEpisodeData);
            Thumbnail = _favEpisodeData.ReviewInfoReview == null ? null : new CachedBitmap(_favEpisodeData.ReviewInfoReview.Thumbnail);
            _dispatcher = Dispatcher.CurrentDispatcher;
            favEpisodeData.PropertyChanged += favEpisodeData_PropertyChanged;

        }

        void favEpisodeData_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ReviewInfoReview")
            {
                _dispatcher.Invoke(delegate
                {
                    Thumbnail = _favEpisodeData.ReviewInfoReview == null ? null : new CachedBitmap(_favEpisodeData.ReviewInfoReview.Thumbnail);
                });

            }
        }

        public FavEpisodeData Episode { get { return _favEpisodeData; } }


        public EpisodeViewModel EpisodeViewModel
        {
            get { return _vm; }
        }

        public CachedBitmap Thumbnail
        {
            get
            {
                return _thumbnail;
            }
            private set
            {
                _thumbnail = value; 
                OnPropertyChanged();
            }
        }

        /* public string DetailTitle
        {
            get
            {
                String s = EpisodeDescriptors.First().SeasonShowName + " ";
                if (EpisodeDescriptors.First().Season != -1)
                {
                    s += "Season " + EpisodeDescriptors.First().Season + " ";
                    if (Episode != -1)
                    {
                        s += "Episode " + Episode;
                    }
                }
                return s;
            }
        }*/
        public string Title
        {
            get
            {
                if (_favEpisodeData.Season.Number == -1)
                {
                    return _favEpisodeData.Name;
                }
                if (_favEpisodeData.Number== -1)
                {
                    return _favEpisodeData.Downloads.Count() + " Others";
                }
                return "Episode " + _favEpisodeData.Number;

            }
        }

        public string Languages
        {
            get
            {
                UploadLanguage langs = _favEpisodeData.Downloads.Aggregate<DownloadData, UploadLanguage>(0, (current, download) => current | download.Upload.Language);

                switch (langs)
                {
                    case UploadLanguage.English:
                        return "English";
                    case UploadLanguage.German:
                        return "German";
                    case UploadLanguage.Any:
                        return "German,English";
                }
                return "";

            }
        }

        public string Formats
        {

            get
            {
                var lisFormats = new List<string>();
                var lisFormatsComp = new List<string>();
                foreach (var downloads in _favEpisodeData.Downloads)
                {
                    string f = downloads.Upload.Format;
                    if (String.IsNullOrWhiteSpace(f))
                        continue;
                    if (!lisFormatsComp.Contains(f.ToLower()))
                    {
                        lisFormats.Add(f);
                        lisFormatsComp.Add(f.ToLower());
                    }
                }

                return string.Join(",", lisFormats);

            }
        }


        public Brush Background
        {
            get
            {

                var colors = new Color[]
                {
                    Color.FromRgb(111, 189, 69),
                    Color.FromRgb(75, 179, 221),
                    Color.FromRgb(65, 100, 165),
                    Color.FromRgb(225, 32, 38),
                    Color.FromRgb(128, 0, 128),
                    Color.FromRgb(0, 128, 64),
                   Color.FromRgb(0, 148, 255),
                    Color.FromRgb(255, 0, 199),
                    Color.FromRgb(255, 135, 15),
                   Color.FromRgb(45, 255, 87),
                    Color.FromRgb(127, 0, 55)
                };

                int season = _favEpisodeData.Season.Number;
                if (season == -1) season = 0;
                Color c = colors[season % colors.Length];
                Color cb = Colors.Black;
                float a = (_favEpisodeData.Number == -1) ? 0.8f : 0.5f;
                byte r = (byte)(a * cb.R + (1 - a) * c.R);
                byte g = (byte)(a * cb.G + (1 - a) * c.G);
                byte b = (byte)(a * cb.B + (1 - a) * c.B);
                return new SolidColorBrush(Color.FromRgb(r, g, b));


            }
        }

        

        public bool IsDoubleWidth
        {
            get
            {
                return _favEpisodeData.Number == -1;
            }
        }
    }
}