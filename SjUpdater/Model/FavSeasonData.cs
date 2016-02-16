using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Xml.Serialization;
using SjUpdater.Provider;
using SjUpdater.Utils;

namespace SjUpdater.Model
{
    public class FavSeasonData : PropertyChangedImpl,  Database.IDatabaseCompatibility
    {
        [Key]
        public int Id { get; set; }

        [NotMapped]
        [XmlIgnore]
        public bool InDatabase { get; set; }

        // private string _name;
        private int _number;
        private int _numberNonEpisodes;
        private int _numberEpisodes;
        private ObservableCollection<FavEpisodeData> _episodes;
        private ObservableCollection<DownloadData> _nonEpisodes; 
        private FavShowData _show;
       // private SeasonInformation _seasonInformation;

        public FavSeasonData()
        {
            InDatabase = false;

            _episodes = new ObservableCollection<FavEpisodeData>();
            _nonEpisodes = new ObservableCollection<DownloadData>();
            _number = -1;
            _episodes.CollectionChanged += _episodes_CollectionChanged;
            _nonEpisodes.CollectionChanged += _nonEpisodes_CollectionChanged;

        }

       /* public SeasonInformation SeasonInformation
        {
            get { return _seasonInformation; }
            set
            {
                _seasonInformation = value;
                OnPropertyChanged();
            }
        }*/

        public int ShowId { get; set; }
        [ForeignKey("ShowId")]
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
                NumberOfEpisodes = _episodes.Count();
                OnPropertyChanged();
            }
        }
        public ObservableCollection<DownloadData> NonEpisodes
        {
            get { return _nonEpisodes; }
            internal set
            {
                if (_nonEpisodes != null)
                    _nonEpisodes.CollectionChanged -= _nonEpisodes_CollectionChanged;
                _nonEpisodes = value;
                _nonEpisodes.CollectionChanged += _nonEpisodes_CollectionChanged;
                NumberOfNonEpisodes = _nonEpisodes.Count();
                OnPropertyChanged();
            }
        }

        [NotMapped]
        [XmlIgnore]
        public int NumberOfEpisodes
        {
            get { return _numberEpisodes; }
            internal set
            {
                if (value == _numberEpisodes) return;
                _numberEpisodes = value;
                OnPropertyChanged();
            }
        }


        [NotMapped]
        [XmlIgnore]
        public int NumberOfNonEpisodes
        {
            get { return _numberNonEpisodes; }
            internal set
            {
                if (value == _numberNonEpisodes) return;
                _numberNonEpisodes = value;
                OnPropertyChanged();
            }
        }

        void _episodes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            NumberOfEpisodes = _episodes.Count();
        }
        void _nonEpisodes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            NumberOfNonEpisodes = _nonEpisodes.Count();
        }

        public void ConvertToDatabase(bool cascade = true)
        {
            if (cascade)
            {
                foreach (FavEpisodeData episode in Episodes)
                {
                    episode.ConvertToDatabase();
                }

                foreach (DownloadData nonEpisode in NonEpisodes)
                {
                    nonEpisode.ConvertToDatabase();
                }
            }
        }

        public void ConvertFromDatabase(bool cascade = true)
        {
            InDatabase = true;

            if (cascade)
            {
                foreach (FavEpisodeData episode in Episodes)
                {
                    episode.ConvertFromDatabase();
                }

                foreach (DownloadData nonEpisode in NonEpisodes)
                {
                    nonEpisode.ConvertFromDatabase();
                }
            }
        }

        public void AddToDatabase(Database.CustomDbContext db)
        {
            if (db == null)
                return;

            if (!InDatabase)
            {
                InDatabase = true;
                ConvertToDatabase(false);

                if (Show != null)
                    Show.AddToDatabase(db);

                foreach (FavEpisodeData episode in Episodes)
                {
                    episode.AddToDatabase(db);
                }

                foreach (DownloadData nonEpisode in NonEpisodes)
                {
                    nonEpisode.AddToDatabase(db);
                }

                Database.DatabaseWriter.AddToDatabase<FavSeasonData>(db.FavSeasonData, this);
            }
        }

        public void RemoveFromDatabase(Database.CustomDbContext db)
        {
            if (db == null)
                return;

            if (InDatabase)
            {
                InDatabase = false;

                if (Show != null)
                    Show.RemoveFromDatabase(db);

                foreach (FavEpisodeData episode in Episodes.ToList())
                {
                    episode.RemoveFromDatabase(db);
                }

                foreach (DownloadData nonEpisode in NonEpisodes.ToList())
                {
                    nonEpisode.RemoveFromDatabase(db);
                }

                Database.DatabaseWriter.RemoveFromDatabase<FavSeasonData>(db.FavSeasonData, this);
            }
        }
    }
}
