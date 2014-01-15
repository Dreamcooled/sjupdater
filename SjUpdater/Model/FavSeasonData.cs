using System.Collections.ObjectModel;
using SjUpdater.Utils;

namespace SjUpdater.Model
{
    public class FavSeasonData :PropertyChangedImpl
    {
       // private string _name;
        private int _number;
        private ObservableCollection<FavEpisodeData> _episodes;
        private FavShowData _show;

        public FavSeasonData()
        {
            _episodes = new ObservableCollection<FavEpisodeData>();
            _number = -1;
        }

        public FavShowData Show
        {
            get { return _show; }
            set
            {
                _show = value;
                OnPropertyChanged();
            }
        }

     /*   public String Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }*/

        public int Number
        {
            get { return _number; }
            set
            {
                _number = value; 
                OnPropertyChanged();
            }
        }

        public ObservableCollection<FavEpisodeData> Episodes
        {
            get { return _episodes; }
            internal set
            {
                _episodes = value;
                OnPropertyChanged();
            }
        }

 
    }
}
