using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MahApps.Metro.Controls;
using SjUpdater.Model;
using SjUpdater.Utils;

namespace SjUpdater.ViewModel
{
    public class SeasonViewModel : PropertyChangedImpl
    {
        private readonly ObservableCollection<EpisodeViewModel> _lisEpisodes;
        private readonly FavSeasonData _season;
        private readonly Dispatcher _dispatcher;
        private bool _isExpanded;
        private static readonly Comparer<EpisodeViewModel> EpisodeComparer =
           Comparer<EpisodeViewModel>.Create(delegate(EpisodeViewModel m1, EpisodeViewModel m2)
           {
               if (m1.Episode.Number == m2.Episode.Number) return 0;

               if (Settings.Instance.SortEpisodesDesc)
               {
                   if (m1.Episode.Number > m2.Episode.Number) return -1;
                   return 1;
               }
               else
               {
                   if (m1.Episode.Number > m2.Episode.Number) return 1;
                   return -1;
               }

           });
        public SeasonViewModel(FavSeasonData season)
        {
            _season = season;
            _dispatcher = Dispatcher.CurrentDispatcher;


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

            season.Episodes.CollectionChanged+=update_source ;

            _lisEpisodes = new ObservableCollection<EpisodeViewModel>();
           
            foreach (FavEpisodeData favEpisodeData in season.Episodes)
            {
                var x = new EpisodeViewModel(favEpisodeData);
                _lisEpisodes.Add(x);
            }
            NonEpisodes = Season.NonEpisodes;

            _lisEpisodes.Sort(EpisodeComparer);

        }

        public String Name
        {
            get { return "Season " + _season.Number; }
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value == _isExpanded) return;
                _isExpanded = value;
                OnPropertyChanged();
            }
        }


        public ICommand DownloadCommand { get; private set; }

        public FavSeasonData Season {
            get { return _season; }
        }

        public ObservableCollection<EpisodeViewModel> Episodes
        {
            get { return _lisEpisodes; }
        }

        public String EpisodeCount
        {
            get
            {

                int e = _season.NumberOfEpisodes;
                int n = _season.NumberOfNonEpisodes;
                if(e>0 && n==0)
                    return e + " Episodes";
                if (n > 0 && e == 0)
                    return n + " Others";
                return e + " Episodes + " + n+ " Others";
            }
        }

        public ObservableCollection<DownloadData> NonEpisodes
        {
            get; private set;
        }


         public CachedBitmap Cover
        {
             get
             {
                 //TODO: fix
                 String url = _season.Episodes.First().Downloads.First().Upload.Season.CoverUrl;
                 return new CachedBitmap(url);
             }
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
                            var favEpisodeData = newItem as FavEpisodeData;
                            _lisEpisodes.Add(new EpisodeViewModel(favEpisodeData));
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        foreach (var oldItem in e.OldItems)
                        {
                            var o = oldItem as FavShowData;
                            for (int i = _lisEpisodes.Count - 2; i >= 0; i--)
                            {
                                if (_lisEpisodes[i].Episode == oldItem)
                                {
                                    _lisEpisodes.RemoveAt(i);
                                }
                            }
                        }
                        break;
                    default:
                        throw new InvalidOperationException(e.Action.ToString());

                }
                _lisEpisodes.Sort(EpisodeComparer);
                
            });
           
        }
    }
}
