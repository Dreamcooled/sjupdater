using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MahApps.Metro.Controls;
using SjUpdater.Model;
using SjUpdater.Utils;

namespace SjUpdater.ViewModel
{
    public class SeasonPanoramaViewModel : PanoramaGroup
    {
        private readonly ObservableCollection<object> _lisTiles;
        private readonly FavSeasonData _season;
        private readonly Dispatcher _dispatcher;
        private static readonly Comparer<object> EpisodeComparer =
           Comparer<object>.Create(delegate(object o1, object o2)
           {
               if (!(o1 is EpisodeTileViewModel)) return 1;
               if (!(o2 is EpisodeTileViewModel)) return -1;
               var m1 = o1 as EpisodeTileViewModel;
               var m2 = o2 as EpisodeTileViewModel;

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
        public SeasonPanoramaViewModel(FavSeasonData season) :base((season.Number==-1)?"Others":("Season "+season.Number))
        {
            _season = season;
            _dispatcher = Dispatcher.CurrentDispatcher;

            season.Episodes.CollectionChanged+=update_source ;

            _lisTiles = new ObservableCollection<object>();
            SetSource(_lisTiles);
            foreach (FavEpisodeData favEpisodeData in season.Episodes)
            {
                var x = new EpisodeTileViewModel(favEpisodeData);//, _openShowCommand);
                _lisTiles.Add(x);
            }



            var t = new Tile();
            t.Content = new TextBlock()
            {
                Text = "*",
                FontSize = 180,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(0),
                Margin = new Thickness(0, -40, 0, 0)
            };
            t.Width = 120;
            t.Height = 120;
           /* Binding b = new Binding("AddShowCommand");
            b.ElementName = "Window";
            t.SetBinding(ButtonBase.CommandProperty, b);*/

            _lisTiles.Add(t);
            _lisTiles.Sort(EpisodeComparer);



        }

        public FavSeasonData Season {
            get { return _season; }
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
                            _lisTiles.Add(new EpisodeTileViewModel(newItem as FavEpisodeData));//,_openShowCommand));
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        foreach (var oldItem in e.OldItems)
                        {
                            var o = oldItem as FavShowData;
                            for (int i = _lisTiles.Count - 2; i >= 0; i--)
                            {
                                if (((EpisodeTileViewModel)_lisTiles[i]).Episode == oldItem)
                                {
                                    _lisTiles.RemoveAt(i);
                                }
                            }
                        }
                        break;
                    default:
                        throw new InvalidOperationException(e.Action.ToString());

                }
                _lisTiles.Sort(EpisodeComparer);
                
            });
           
        }
    }
}
