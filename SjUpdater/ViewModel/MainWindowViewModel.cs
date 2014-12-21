using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using SjUpdater.Model;
using SjUpdater.Utils;

namespace SjUpdater.ViewModel
{
    public class MainWindowViewModel
    {

        public MainWindowViewModel(ObservableCollection<FavShowData> shows)
        {
            _tvShows = new ObservableCollection<ShowTileViewModel>();

            shows.CollectionChanged += update_source;

            foreach (FavShowData favShowData in shows)
            {
                var x = new ShowTileViewModel(favShowData);
                _tvShows.Add(x);
            }
            if (Settings.Instance.SortShowsAlphabetically)
                _tvShows.Sort(ShowComparer);

        }


        private readonly ObservableCollection<ShowTileViewModel> _tvShows;

        private static readonly Comparer<ShowTileViewModel> ShowComparer =
         Comparer<ShowTileViewModel>.Create((m1, m2) => String.CompareOrdinal(m1.Title, m2.Title));


        private void update_source(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var newItem in e.NewItems)
                    {
                        _tvShows.Insert(_tvShows.Count, new ShowTileViewModel(newItem as FavShowData));
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var oldItem in e.OldItems)
                    {
                        var o = oldItem as FavShowData;
                        for (int i = _tvShows.Count - 1; i >= 0; i--)
                        {
                            if (_tvShows[i].Show == oldItem)
                            {
                                _tvShows.RemoveAt(i);
                            }
                        }
                    }
                    break;
                default:
                    throw new InvalidOperationException(e.Action.ToString());


            }
        }


        public ObservableCollection<ShowTileViewModel> TvShows
        {
            get { return _tvShows; }
        } 


    }
}
