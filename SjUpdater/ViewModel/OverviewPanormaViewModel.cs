using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using MahApps.Metro.Controls;
using SjUpdater.Model;
using SjUpdater.Utils;

namespace SjUpdater.ViewModel
{
    public class OverviewPanormaViewModel:PanoramaGroup
    {
        private readonly ObservableCollection<FavShowData> _shows;
        private readonly ObservableCollection<ShowTileViewModel> _lisTiles;

        private static readonly Comparer<ShowTileViewModel> ShowComparer =
          Comparer<ShowTileViewModel>.Create((m1, m2) => String.CompareOrdinal(m1.Title, m2.Title));

        public OverviewPanormaViewModel(ObservableCollection<FavShowData>  shows) : base("My TV Shows")
        {
            _shows = shows;


            shows.CollectionChanged += update_source;

            _lisTiles = new ObservableCollection<ShowTileViewModel>();
            SetSource(_lisTiles);
            foreach (FavShowData favShowData in _shows)
            {
                var x = new ShowTileViewModel(favShowData);
                _lisTiles.Add(x);
            }
            if (Settings.Instance.SortShowsAlphabetically)
                _lisTiles.Sort(ShowComparer);

        }

        private void update_source(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var newItem in e.NewItems)
                    {
                        _lisTiles.Insert(_lisTiles.Count,new ShowTileViewModel(newItem as FavShowData));
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var oldItem in e.OldItems)
                    {
                        var o = oldItem as FavShowData;
                        for (int i=_lisTiles.Count-1; i>=0; i--)
                        {
                            if (_lisTiles[i].Show == oldItem)
                            {
                                _lisTiles.RemoveAt(i);
                            }
                        }
                    }
                    break;
                default:
                    throw new InvalidOperationException(e.Action.ToString());


            }
        }
    }
}
