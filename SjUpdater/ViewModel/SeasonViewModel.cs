using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
        private static readonly Comparer<EpisodeViewModel> EpisodeComparer =
           Comparer<EpisodeViewModel>.Create(delegate(EpisodeViewModel m1, EpisodeViewModel m2)
           {
               if (m1.Episode.Number == m2.Episode.Number) return 0;
               if (m1.Episode.Number == -1) return 1;
               if (m2.Episode.Number == -1) return -1;

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

            season.Episodes.CollectionChanged+=update_source ;

            _lisEpisodes = new ObservableCollection<EpisodeViewModel>();
           
            foreach (FavEpisodeData favEpisodeData in season.Episodes)
            {
                if(favEpisodeData.Number==-1) continue;
                var x = new EpisodeViewModel(favEpisodeData);
                _lisEpisodes.Add(x);
            }

            _lisEpisodes.Sort(EpisodeComparer);

        }

        public String Name
        {
            get { return ((_season.Number == -1) ? "Others" : ("Season " + _season.Number)); }
        }



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
                int c= _season.Episodes.Count(episode => episode.Number != -1);
                return c + " Episodes";
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
                            if (favEpisodeData.Number == -1) continue;
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
