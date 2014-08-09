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
                var x = new ShowTileViewModel(favShowData);//, _openShowCommand);
                _lisTiles.Add(x);
            }
            if (Settings.Instance.SortShowsAlphabetically)
                _lisTiles.Sort(ShowComparer);


         
           /*  var t =  new Tile();
            t.Content = new TextBlock()
            {
                Text = "+",
                FontSize = 180,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(0),
                Margin = new Thickness(0, -40, 0, 0)
            };
            t.Width = 120;
            t.Height = 120;
            Binding b = new Binding("AddShowCommand");
            b.ElementName = "Window";
            t.SetBinding(ButtonBase.CommandProperty, b);

            _lisTiles.Add(t);*/
            //{Binding ElementName=Window, Path=ShowClickedCommand }
            /* Command = new SimpleCommand<object, object>(o =>
             {
                 if (AddNew != null)
                     AddNew(this, EventArgs.Empty);
             })*/





        }

        private void update_source(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var newItem in e.NewItems)
                    {
                        _lisTiles.Insert(_lisTiles.Count-1,new ShowTileViewModel(newItem as FavShowData));//,_openShowCommand));
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var oldItem in e.OldItems)
                    {
                        var o = oldItem as FavShowData;
                        for (int i=_lisTiles.Count-2; i>=0; i--)
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
