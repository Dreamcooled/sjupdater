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


        public static readonly List<ShowCategorySetting> DefaultSettings  = new List<ShowCategorySetting>()
        {
            new ShowCategorySetting("new",CategoryOrderingType.DatePrev),
            new ShowCategorySetting("update",CategoryOrderingType.DatePrev),
            new ShowCategorySetting("active",CategoryOrderingType.DateNextPrev),
            new ShowCategorySetting("ended",CategoryOrderingType.DatePrev),
            new ShowCategorySetting("unknown",CategoryOrderingType.Alphabetical),
            new ShowCategorySetting("all",CategoryOrderingType.Alphabetical)
        };



        public String Title { get; internal set; }

        public ObservableCollection<ShowTileViewModel> Shows { get; } = new ObservableCollection<ShowTileViewModel>();
        public ShowCategorySetting Setting { get; internal set; }

        public void AddShow(ShowTileViewModel show)
        {
            if (!Shows.Contains(show))
            {
                Shows.Add(show);
                show.Show.PropertyChanged += Show_PropertyChanged;
                Setting.Sort(Shows);
            }
        }

        public void Sort()
        {
            Setting.Sort(Shows);
        }

        private void Show_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(FavShowData.NextEpisodeDate) || args.PropertyName == nameof(FavShowData.PreviousEpisodeDate))
            {
                _dispatcher.Invoke(delegate
                {
                    Setting.Sort(Shows);
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
