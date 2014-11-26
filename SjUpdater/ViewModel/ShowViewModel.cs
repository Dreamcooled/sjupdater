using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using SjUpdater.Model;
using SjUpdater.Utils;

namespace SjUpdater.ViewModel
{
    public class ShowViewModel :  PropertyChangedImpl
    {
        private readonly FavShowData _show;
        private readonly ObservableCollection<SeasonViewModel> _lisSeasons;
        private readonly Dispatcher _dispatcher;
        private CachedBitmap _bitmap;
        private String _description;


        private static readonly Comparer<SeasonViewModel> SeasonComparer = 
            Comparer<SeasonViewModel>.Create( delegate(SeasonViewModel m1, SeasonViewModel m2)
            {
                if (m1.Season.Number == m2.Season.Number) return 0;
                if (m1.Season.Number == -1) return 1;
                if (m2.Season.Number == -1) return -1;
                if (Settings.Instance.SortSeasonsDesc)
                {
                    if (m1.Season.Number > m2.Season.Number) return -1;
                    return 1;
                }
                else
                {
                    if (m1.Season.Number > m2.Season.Number) return 1;
                    return -1;
                }
            });

        public ShowViewModel(FavShowData show)
        {
            _show = show;
            _dispatcher = Dispatcher.CurrentDispatcher;
            show.Seasons.CollectionChanged += update_source;

            _lisSeasons = new ObservableCollection<SeasonViewModel>();

            Cover = !String.IsNullOrWhiteSpace(_show.Cover) ? new CachedBitmap(_show.Cover) : null;
            Description = (_show.Seasons.Any(s => s.Episodes.Any(e => e.Downloads.Any()))) ?
                _show.Seasons.First(s => s.Episodes.Any(e => e.Downloads.Any())).Episodes.First(e => e.Downloads.Any()).Downloads.First().Upload.Season.Description : "";

            foreach (FavSeasonData favSeasonData in show.Seasons)
            {
                if(favSeasonData.Number==-1) continue;
                var x = new SeasonViewModel(favSeasonData);
                _lisSeasons.Add(x);
            }
            _lisSeasons.Sort(SeasonComparer);


            UnmarkAllCommand = new SimpleCommand<object, object>(o =>
            {
                foreach (var season in Show.Seasons)
                {
                    if (season.Number != -1)
                    {
                        foreach (var episode in season.Episodes)
                        {
                            if (episode.Number != -1)
                            {
                                episode.Downloaded = false;
                                episode.Watched = false;
                            }
                        }
                    }
                }
            });
            MarkAllDownloadedCommand = new SimpleCommand<object, object>(o =>
            {
                foreach (var season in Show.Seasons)
                {
                    if (season.Number != -1)
                    {
                        foreach (var episode in season.Episodes)
                        {
                            if (episode.Number != -1)
                            {
                                episode.Downloaded = true;
                                //episode.Watched = false; //not sure here
                            }
                        }
                    }
                }
            });
            MarkAllWatchedCommand = new SimpleCommand<object, object>(o =>
            {
                foreach (var season in Show.Seasons)
                {
                    if (season.Number != -1)
                    {
                        foreach (var episode in season.Episodes)
                        {
                            if (episode.Number != -1)
                            {
                                episode.Downloaded = true;
                                episode.Watched = true;
                            }
                        }
                    }
                }
            });
        }

        private void update_source(object sender, NotifyCollectionChangedEventArgs e)
        {
            _dispatcher.Invoke(delegate
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (var newItem in e.NewItems)
                        {
                            var favSeasonData = newItem as FavSeasonData;
                            if(favSeasonData.Number!=-1)
                            _lisSeasons.Add(new SeasonViewModel(favSeasonData));
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        foreach (var oldItem in e.OldItems)
                        {
                            var o = oldItem as FavShowData;
                            for (int i = _lisSeasons.Count - 2; i >= 0; i--)
                            {
                                if (_lisSeasons[i].Season == oldItem)
                                {
                                    _lisSeasons.RemoveAt(i);
                                    break;
                                }
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        _lisSeasons.Clear();
                        foreach (FavSeasonData favSeasonData in _show.Seasons)
                        {
                             if(favSeasonData.Number==-1) continue;
                            var x = new SeasonViewModel(favSeasonData);
                            _lisSeasons.Add(x);
                        }
                        break;
                    default:
                        throw new InvalidOperationException(e.Action.ToString());

                }
                _lisSeasons.Sort(SeasonComparer);

            });
          
        }


        public ObservableCollection<SeasonViewModel> Seasons
        {
            get { return _lisSeasons; }
        }

        public String Title
        {
            get
            {
                return _show.Name;
            }
        }

        public CachedBitmap Cover
        {
            get { return _bitmap; }

            private set
            {
                _bitmap = value;
                OnPropertyChanged();
            }
        }

        public String Description
        {
            get { return _description; }

            private set
            {
                _description = value;
                OnPropertyChanged();
            }
        }




        public String FilterName
        {
            get { return _show.FilterName; }
            set
            {
                _show.FilterName = value;
                OnPropertyChanged();
            }
        }

        public String FilterFormat
        {
            get { return _show.FilterFormat; }
            set
            {
                _show.FilterFormat = value;
                OnPropertyChanged();
            }
        }

        public String FilterUploader
        {
            get { return _show.FilterUploader; }
            set
            {
                _show.FilterUploader = value;
                OnPropertyChanged();
            }
        }

        public bool FilterNonSeason
        {
            get { return _show.FilterShowNonSeason.GetValueOrDefault(); }
            set
            {
                _show.FilterShowNonSeason = value;
                OnPropertyChanged();
            }
        }

        public bool FilterNonEpisode
        {
            get { return _show.FilterShowNonEpisode.GetValueOrDefault(); }
            set
            {
                _show.FilterShowNonEpisode = value;
                OnPropertyChanged();
            }
        }

        public String FilterSize
        {
            get { return _show.FilterSize; }
            set
            {
                _show.FilterSize = value;
                OnPropertyChanged();
            }
        }

        public String FilterRuntime
        {
            get { return _show.FilterSize; }
            set
            {
                _show.FilterSize = value;
                OnPropertyChanged();
            }
        }

        public UploadLanguage FilterLanguage
        {
            get { return _show.FilterLanguage.GetValueOrDefault(); }
            set
            {
                _show.FilterLanguage = value;
                OnPropertyChanged();
            }
        }

        public String FilterHoster
        {
            get { return _show.FilterHoster; }
            set
            {
                _show.FilterHoster = value;
                OnPropertyChanged();
            }
        }


  

        public FavShowData Show { get { return _show; } }

        public ICommand UnmarkAllCommand { get; private set; }
        public ICommand MarkAllDownloadedCommand { get; private set; }
        public ICommand MarkAllWatchedCommand { get; private set; }
    }
}
