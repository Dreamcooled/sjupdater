using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MahApps.Metro.Converters;
using SjUpdater.Model;
using SjUpdater.Utils;

namespace SjUpdater.ViewModel
{
    public class EpisodeViewModel : PropertyChangedImpl
    {
        private readonly FavEpisodeData _favEpisodeData;
        private CachedBitmap _thumbnail;
        private readonly Dispatcher _dispatcher;
        private Visibility _newEpisodeVisible;
        private Visibility _newUpdateVisible;
        private Visibility _downloadedCheckVisible;
        private Visibility _watchedCheckVisible;

        public EpisodeViewModel(FavEpisodeData favEpisodeData)
        {
            _favEpisodeData = favEpisodeData;
            Thumbnail = _favEpisodeData.ReviewInfoReview == null ? null : new CachedBitmap(_favEpisodeData.ReviewInfoReview.Thumbnail);
            NewEpisodeVisible = (_favEpisodeData.NewEpisode) ? Visibility.Visible : Visibility.Collapsed;
            NewUpdateVisible = (_favEpisodeData.NewUpdate) ? Visibility.Visible : Visibility.Collapsed;
            DownloadedCheckVisibility = (_favEpisodeData.Downloaded) ? Visibility.Visible : Visibility.Collapsed;
            WatchedCheckVisibility = (_favEpisodeData.Watched) ? Visibility.Visible : Visibility.Collapsed;
            _dispatcher = Dispatcher.CurrentDispatcher;
            favEpisodeData.PropertyChanged += favEpisodeData_PropertyChanged;

            ReviewCommand = new SimpleCommand<object, object>(b => (_favEpisodeData.ReviewInfoReview != null), delegate
            {
                var p = new Process();
                p.StartInfo.FileName = _favEpisodeData.ReviewInfoReview.ReviewUrl;
                p.Start();
                Stats.TrackAction(Stats.TrackActivity.Review);
            });

            StateChangeCommand = new SimpleCommand<object, object>(o =>
            {
                if (!_favEpisodeData.Downloaded && !_favEpisodeData.Watched)
                {
                    _favEpisodeData.Downloaded = true;
                    return;
                }
                if (_favEpisodeData.Downloaded && !_favEpisodeData.Watched)
                {
                    _favEpisodeData.Watched = true;
                    return;
                }
                _favEpisodeData.Downloaded = false;
                _favEpisodeData.Watched = false;
            });

            DownloadCommand = new SimpleCommand<object, String>(s =>
            {
                if (_favEpisodeData.Number != -1 && _favEpisodeData.Season.Number != -1)
                {
                    _favEpisodeData.Downloaded = true;
                }
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        Clipboard.SetText(s);
                        Clipboard.Flush();
                        Stats.TrackAction(Stats.TrackActivity.Download);
                        return;
                    }
                    catch
                    {
                        //nah
                    }
                    Thread.Sleep(10);
                }
                MessageBox.Show("Couldn't Copy link to clipboard.\n" + s);
            });

        }

        void favEpisodeData_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ReviewInfoReview")
            {
                _dispatcher.Invoke(delegate
                {
                    Thumbnail = _favEpisodeData.ReviewInfoReview == null ? null : new CachedBitmap(_favEpisodeData.ReviewInfoReview.Thumbnail);
                });

            } else if (e.PropertyName == "NewEpisode" || e.PropertyName=="NewUpdate")
            {
                NewEpisodeVisible = (_favEpisodeData.NewEpisode) ? Visibility.Visible : Visibility.Collapsed;
                NewUpdateVisible = (_favEpisodeData.NewUpdate) ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (e.PropertyName == "Downloaded")
            {
                DownloadedCheckVisibility = (_favEpisodeData.Downloaded) ? Visibility.Visible : Visibility.Collapsed;
                OnPropertyChanged("ButtonStateChangeText");
            }
            else if (e.PropertyName == "Watched")
            {
                WatchedCheckVisibility = (_favEpisodeData.Watched) ? Visibility.Visible : Visibility.Collapsed;
                OnPropertyChanged("ButtonStateChangeText");
            }

        }

        public ICommand DownloadCommand { get; private set; }

        public FavEpisodeData Episode { get { return _favEpisodeData; } }


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

        public Visibility NewEpisodeVisible
        {
            get { return _newEpisodeVisible; }

            private set
            {
                _newEpisodeVisible = value;
                OnPropertyChanged();
            }
        }

        public Visibility NewUpdateVisible
        {
            get { return _newUpdateVisible; }

            private set
            {
                _newUpdateVisible = value;
                OnPropertyChanged();
            }
        }


        public Visibility DownloadedCheckVisibility
        {
            get { return _downloadedCheckVisible; }

            private set
            {
                _downloadedCheckVisible = value;
                OnPropertyChanged();
            }
        }

        public Visibility WatchedCheckVisibility
        {
            get { return _watchedCheckVisible; }

            private set
            {
                _watchedCheckVisible = value;
                OnPropertyChanged();
            }
        }

        public ICommand ReviewCommand { get; private set; }

        public ICommand StateChangeCommand { get; private set; }
        public String ButtonStateChangeText
        {
            get
            {
                if (!_favEpisodeData.Downloaded && !_favEpisodeData.Watched)
                {
                    return "Mark as Downloaded";
                }
                if (_favEpisodeData.Downloaded && !_favEpisodeData.Watched)
                {
                    return "Mark as Watched";
                }
                return "Unmark";
            }
        }


        public String Title
        {
            get
            {

                if (_favEpisodeData.ReviewInfoReview != null)
                {
                    return _favEpisodeData.ReviewInfoReview.Name;
                }

                return "Episode " + _favEpisodeData.Number;
               
            }
        }

        public int Number
        {
            get { return _favEpisodeData.Number; }
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
                for (int i = 0; i < _favEpisodeData.Downloads.Count; i++) //because collection might change in another thread, we have to use for instead of foreach
                {
                    var downloads = _favEpisodeData.Downloads[i];
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

        public List<DownloadData> FavorizedUploads
        {
            get { return _favEpisodeData.Downloads.Where(d => d.Upload.Favorized).OrderByDescending(d => d.Upload.Size).ToList(); }
        }

        public ObservableCollection<DownloadData> Downloads
        {
            get { return _favEpisodeData.Downloads; }
        }
    }
}