using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using SjUpdater.Model;
using SjUpdater.Utils;

namespace SjUpdater.ViewModel
{
    public class EpisodeViewModel : PropertyChangedImpl
    {
        private readonly FavEpisodeData _episode;

        public EpisodeViewModel(FavEpisodeData favEpisodeData)
        {
            _episode = favEpisodeData;
            _episode.PropertyChanged += _episode_PropertyChanged;
            ReviewCommand = new SimpleCommand<object, object>(b => (_episode.ReviewInfoReview != null), delegate
            {
                var p = new Process();
                p.StartInfo.FileName = _episode.ReviewInfoReview.ReviewUrl;
                p.Start();
                Stats.TrackAction(Stats.TrackActivity.Review);
            });

            DownloadCommand = new SimpleCommand<object, String>(s =>
            {
                if (_episode.Number != -1 && _episode.Season.Number != -1)
                {
                    _episode.Downloaded = true;
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

            StateChangeCommand = new SimpleCommand<object, object>(o =>
            {
                if (!_episode.Downloaded && !_episode.Watched)
                {
                    _episode.Downloaded = true;
                    return;
                }
                if (_episode.Downloaded && !_episode.Watched)
                {
                    _episode.Watched = true;
                    return;
                }
                _episode.Downloaded = false;
                _episode.Watched = false;
            });

        }

        void _episode_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Downloaded")
            {
                OnPropertyChanged("DownloadedCheckVisibility");
            }
            else if (e.PropertyName == "Watched")
            {
                OnPropertyChanged("WatchedCheckVisibility");
            }
            if (e.PropertyName == "Downloaded" || e.PropertyName == "Watched")
            {
                OnPropertyChanged("ButtonStateChangeText");
            }
        }
     

        public ICommand DownloadCommand { get; private set; }

        public CachedBitmap Thumbnail
        {
            get
            {
                if (_episode.ReviewInfoReview == null)
                {
                    return null;
                }
                else
                {
                    return new CachedBitmap(_episode.ReviewInfoReview.Thumbnail);
                }
            }
        }

        public Visibility MarkStuffVisibility
        {
            get { return (_episode.Number!=-1 && _episode.Season.Number!=-1 )? Visibility.Visible : Visibility.Collapsed; }

        }


        public Visibility DownloadedCheckVisibility
        {
            get { return _episode.Downloaded?Visibility.Visible : Visibility.Collapsed; }

        }

        public Visibility WatchedCheckVisibility
        {
            get { return _episode.Watched ? Visibility.Visible : Visibility.Collapsed; }

        }

        public ICommand ReviewCommand { get; private set; }

        public ICommand StateChangeCommand { get; private set; }
        public String ButtonStateChangeText
        {
            get
            {
                if (!_episode.Downloaded && !_episode.Watched)
                {
                    return "Mark as Downloaded";
                }
                if (_episode.Downloaded && !_episode.Watched)
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
                String s = _episode.Season.Show.Name+ " ";
                if (_episode.Season.Number != -1)
                {
                    s += "Season " + _episode.Season.Number + " ";
                    if (_episode.Number != -1)
                    {
                        s += "Episode " + _episode.Number;
                        if (_episode.ReviewInfoReview != null)
                        {
                            s += " - "+_episode.ReviewInfoReview.Name;
                        }
                    }
                }
                
                return s;
            }
        }

        public FavEpisodeData Episode
        {
            get { return _episode; }
        }

        public ObservableCollection<DownloadData> Downloads
        {
            get { return _episode.Downloads; }
        }
    }
}
