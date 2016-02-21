using SjUpdater.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SjUpdater.Model
{
    public class ShowCategory
    {
        private readonly Dispatcher _dispatcher;

        public ShowCategory()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
        }


        public static readonly List<ShowCategorySettings> DefaultSettings  = new List<ShowCategorySettings>()
        {
            new ShowCategorySettings("new",CategoryOrderingType.Date),
            new ShowCategorySettings("update",CategoryOrderingType.Date),
            new ShowCategorySettings("active",CategoryOrderingType.Date),
            new ShowCategorySettings("ended",CategoryOrderingType.Date),
            new ShowCategorySettings("unknown",CategoryOrderingType.Alphabetical),
            new ShowCategorySettings("all",CategoryOrderingType.Alphabetical)
        };



        public String Title { get; internal set; }

        public ObservableCollection<ShowTileViewModel> Shows { get; } = new ObservableCollection<ShowTileViewModel>();
        public ShowCategorySettings Settings { get; internal set; }

        public void AddShow(ShowTileViewModel show)
        {
            if (!Shows.Contains(show))
            {
                Shows.Add(show);
                show.Show.PropertyChanged += Show_PropertyChanged;
                Settings.Sort(Shows);
            }
        }

        public void Sort()
        {
            Settings.Sort(Shows);
        }

        private void Show_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(FavShowData.NextEpisodeDate) || args.PropertyName == nameof(FavShowData.PreviousEpisodeDate))
            {
                _dispatcher.Invoke(delegate
                {
                    Settings.Sort(Shows);
                });
            }
        }

   

        public void RemoveShow(ShowTileViewModel show)
        {
            if (Shows.Contains(show))
            {
                show.Show.PropertyChanged -= Show_PropertyChanged;
                Shows.Remove(show);
            }
        }
    }
}
