using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Threading;
using SjUpdater.Model;
using SjUpdater.Utils;

namespace SjUpdater.ViewModel
{
    public class ShowViewModel :  PropertyChangedImpl
    {
        private readonly FavShowData _show;
        private readonly ObservableCollection<SeasonPanoramaViewModel> _lisSeasons;
        private readonly Dispatcher _dispatcher;

        private static readonly Comparer<SeasonPanoramaViewModel> SeasonComparer = 
            Comparer<SeasonPanoramaViewModel>.Create( delegate(SeasonPanoramaViewModel m1, SeasonPanoramaViewModel m2)
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

            _lisSeasons = new ObservableCollection<SeasonPanoramaViewModel>();

            foreach (FavSeasonData favSeasonData in show.Seasons)
            {
                var x = new SeasonPanoramaViewModel(favSeasonData);//, _openShowCommand);
                _lisSeasons.Add(x);
            }
            _lisSeasons.Sort(SeasonComparer);
          
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
                            _lisSeasons.Add(new SeasonPanoramaViewModel(newItem as FavSeasonData));//,_openShowCommand));
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
                                }
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        _lisSeasons.Clear();
                        foreach (FavSeasonData favSeasonData in _show.Seasons)
                        {
                            var x = new SeasonPanoramaViewModel(favSeasonData);//, _openShowCommand);
                            _lisSeasons.Add(x);
                        }
                        break;
                    default:
                        throw new InvalidOperationException(e.Action.ToString());

                }
                _lisSeasons.Sort(SeasonComparer);

            });
          
        }


        public ObservableCollection<SeasonPanoramaViewModel> PanoramaItems
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
    }
}
