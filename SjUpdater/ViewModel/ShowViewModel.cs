using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
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
        private readonly ObservableCollection<DownloadData> _lisNonSeasons;
        private readonly Dispatcher _dispatcher;
        private CachedBitmap _bitmap;
        private String _description;


        private static readonly Comparer<SeasonViewModel> SeasonComparer = 
            Comparer<SeasonViewModel>.Create( delegate(SeasonViewModel m1, SeasonViewModel m2)
            {
                if (m1.Season.Number == m2.Season.Number) return 0;
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
            show.Seasons.CollectionChanged += UpdateSeasons;
            show.NonSeasons.CollectionChanged += UpdateNonSeasons;

            _lisSeasons = new ObservableCollection<SeasonViewModel>();
            _lisNonSeasons = new ObservableCollection<DownloadData>();

            Cover = !String.IsNullOrWhiteSpace(_show.Cover) ? new CachedBitmap(_show.Cover) : null;
            Description = (_show.Seasons.Any(s => s.Episodes.Any(e => e.Downloads.Any()))) ?
                _show.Seasons.First(s => s.Episodes.Any(e => e.Downloads.Any())).Episodes.First(e => e.Downloads.Any()).Downloads.First().Upload.Season.Description : "";

            foreach (FavSeasonData favSeasonData in show.Seasons)
            {
                var x = new SeasonViewModel(favSeasonData);
                _lisSeasons.Add(x);
            }
            _lisSeasons.Sort(SeasonComparer);
            var first = _lisSeasons.FirstOrDefault();
            if (first != null)
            {
                first.IsExpanded = true;
            }

            foreach (DownloadData nonSeason in show.NonSeasons)
            {
                _lisNonSeasons.Add(nonSeason);
            }


            UnmarkAllCommand = new SimpleCommand<object, object>(o =>
            {
                foreach (var season in Show.Seasons)
                {
                    foreach (var episode in season.Episodes)
                    {
                            episode.Downloaded = false;
                            episode.Watched = false;
                    }
                }
            });
            MarkAllDownloadedCommand = new SimpleCommand<object, object>(o =>
            {
                foreach (var season in Show.Seasons)
                {
                    foreach (var episode in season.Episodes)
                    {
                        episode.Downloaded = true;
                        //episode.Watched = false; //not sure here
                    }
                }
            });
            MarkAllWatchedCommand = new SimpleCommand<object, object>(o =>
            {
                foreach (var season in Show.Seasons)
                {
                    foreach (var episode in season.Episodes)
                    {
                            episode.Downloaded = true;
                            episode.Watched = true;
                    }
                }
            });

            DownloadCommand = new SimpleCommand<object, String>(s =>
            {
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

        private void UpdateSeasons(object sender, NotifyCollectionChangedEventArgs e)
        {
            _dispatcher.Invoke(delegate
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (var newItem in e.NewItems)
                        {
                            var favSeasonData = newItem as FavSeasonData;
                            _lisSeasons.Add(new SeasonViewModel(favSeasonData));
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        foreach (var oldItem in e.OldItems)
                        {
                            var o = oldItem as FavShowData;
                            for (int i = _lisSeasons.Count - 1; i >= 0; i--) 
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

        private void UpdateNonSeasons(object sender, NotifyCollectionChangedEventArgs e)
        {
            _dispatcher.Invoke(delegate
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (var newItem in e.NewItems)
                        {
                            _lisNonSeasons.Add(newItem as DownloadData);
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        foreach (var oldItem in e.OldItems)
                        {
                            var o = oldItem as DownloadData;
                            for (int i = _lisNonSeasons.Count - 1; i >= 0; i--) 
                            {
                                if (_lisNonSeasons[i] == oldItem)
                                {
                                    _lisNonSeasons.RemoveAt(i);
                                    break;
                                }
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        _lisNonSeasons.Clear();
                        foreach (DownloadData nonSeason in _show.NonSeasons)
                        {
                            _lisNonSeasons.Add(nonSeason);
                        }
                        break;
                    default:
                        throw new InvalidOperationException(e.Action.ToString());

                }

            });
        }


        public ObservableCollection<SeasonViewModel> Seasons => _lisSeasons;

        public ObservableCollection<DownloadData> NonSeasons => _lisNonSeasons;

        public String Title => _show.Name;

        public CachedBitmap Cover
        {
            get { return _bitmap; }

            private set
            {
                _bitmap = value;
                OnPropertyChanged();
            }
        }

        public Visibility SeasonImageVisibility => Settings.Instance.EnableImages ? Visibility.Visible : Visibility.Collapsed;

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


  

        public FavShowData Show => _show;

        public ICommand UnmarkAllCommand { get; private set; }
        public ICommand MarkAllDownloadedCommand { get; private set; }
        public ICommand MarkAllWatchedCommand { get; private set; }
        public ICommand DownloadCommand { get; private set; }
    }
}
