using System;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using SjUpdater.Utils;

namespace SjUpdater.Model
{
    public class FavEpisodeData : PropertyChangedImpl
    {
        private string _name;
        private int _number;
        private bool _newEpisode;
        private bool _newUpdate;
        private ObservableCollection<DownloadData> _downloads;
        private FavSeasonData _season;
        private SjDeReview _reviewInfoReview;

        public FavEpisodeData()
        {
            _downloads = new ObservableCollection<DownloadData>();
            _name = "";
            _number = -1;
            _reviewInfoReview = null;
            _newEpisode = false;
            _newUpdate = false;
        }


        public SjDeReview ReviewInfoReview
        {
            get { return _reviewInfoReview; }
            set
            {
                _reviewInfoReview = value;
                OnPropertyChanged();
            }
        }

        public FavSeasonData Season
        {
            get { return _season; }
            set
            {
                _season = value;
                OnPropertyChanged();
            }
        }

        public String Name
        {
            get { return _name; }
            set
            {
                _name = value; 
                OnPropertyChanged();
            }
        }

        public int Number
        {
            get { return _number; }
            set
            {
                _number = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Is set to true when the episode is new. Reset this to false, yourself
        /// </summary>
        [XmlIgnore]
        public bool NewEpisode
        {
            get { return _newEpisode; }
            internal set
            {
                if (value == _newEpisode) return;
                _newEpisode = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Is set to true when the episode received a new Download. Reset this to false, yourself
        /// </summary>
        [XmlIgnore]
        public bool NewUpdate
        {
            get { return _newUpdate; }
            internal set
            {
                if (value == _newUpdate || _newEpisode) return;
                _newUpdate = value;
                OnPropertyChanged();
            }
        }


        public ObservableCollection<DownloadData> Downloads
        {
            get { return _downloads; }
            internal set
            {
                _downloads = value;
                OnPropertyChanged();
            }
        }
    }
}
