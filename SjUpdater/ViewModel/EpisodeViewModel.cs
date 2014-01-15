using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using SjUpdater.Model;
using SjUpdater.Utils;

namespace SjUpdater.ViewModel
{
    public class EpisodeViewModel
    {
        private readonly FavEpisodeData _episode;

        public EpisodeViewModel(FavEpisodeData favEpisodeData)
        {
            _episode = favEpisodeData;
            ReviewCommand = new SimpleCommand<object, object>(b => (_episode.ReviewInfoReview != null), delegate
            {
                var p = new Process();
                p.StartInfo.FileName = _episode.ReviewInfoReview.ReviewUrl;
                p.Start();
            });
        }

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

        public ICommand ReviewCommand
        {
            get;
            private set;
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
