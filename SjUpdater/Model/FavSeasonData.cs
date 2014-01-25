using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Serialization;
using SjUpdater.Utils;

namespace SjUpdater.Model
{
    public class FavSeasonData :PropertyChangedImpl
    {
       // private string _name;
        private int _number;
        private int _nrEpisodes;
        private ObservableCollection<FavEpisodeData> _episodes;
        private FavShowData _show;

        public FavSeasonData()
        {
            _episodes = new ObservableCollection<FavEpisodeData>();
            _number = -1;
            _episodes.CollectionChanged += _episodes_CollectionChanged;
            
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
                if(_episodes!=null)
                    _episodes.CollectionChanged -= _episodes_CollectionChanged;
                _episodes = value;
                _episodes.CollectionChanged += _episodes_CollectionChanged;
                RecountEpisodes();
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        public int NumberOfEpisodes
        {
            get { return _nrEpisodes; }
            internal set
            {
                if (value == _nrEpisodes) return;
                _nrEpisodes = value;
                OnPropertyChanged();
            }
        }

        private void RecountEpisodes()
        {
            NumberOfEpisodes = _episodes.Count(episode => episode.Number != -1);
        }

        void _episodes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RecountEpisodes();
           
        }

    }
}
