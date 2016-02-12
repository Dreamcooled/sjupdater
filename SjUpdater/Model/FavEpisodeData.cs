using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Serialization;
using SjUpdater.Provider;
using SjUpdater.Utils;

namespace SjUpdater.Model
{
    public class FavEpisodeData : PropertyChangedImpl,  Database.IDatabaseCompatibility
    {
        [Key]
        public int Id { get; set; }

        private int _number;
        private bool _newEpisode;
        private bool _newUpdate;
        private bool _watched;
        private bool _downloaded;
        private ObservableCollection<DownloadData> _downloads;
        private FavSeasonData _season;
        private EpisodeInformation _episodeInformation;

        public FavEpisodeData()
        {
            _downloads = new ObservableCollection<DownloadData>();
            _number = -1;
            _episodeInformation = null;
            _newEpisode = false;
            _newUpdate = false;
            _downloaded = false;
            _watched = false;
        }

        public EpisodeInformation EpisodeInformation
        {
            get { return _episodeInformation; }
            set
            {
                _episodeInformation = value;
                OnPropertyChanged();
            }
        }

        [Required]
        public int SeasonId { get; set; }
        [ForeignKey("SeasonId")]
        public FavSeasonData Season
        {
            get { return _season; }
            set
            {
                _season = value;
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

        public bool Watched
        {
            get { return _watched; }
            set
            {
                if (value == _watched)
                    return;
                _watched = value;
                OnPropertyChanged();
            }
        }
        public bool Downloaded
        {
            get { return _downloaded; }
            set
            {
                if (value == _downloaded)
                    return;
                _downloaded = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Is set to true when the episode is new. Reset this to false, yourself
        /// </summary>
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

        public void ConvertToDatabase()
        {
            foreach (DownloadData download in Downloads)
            {
                download.ConvertToDatabase();
            }

            if (EpisodeInformation != null)
                EpisodeInformation.ConvertToDatabase();
        }

        public void ConvertFromDatabase()
        {
            foreach (DownloadData download in Downloads)
            {
                download.ConvertFromDatabase();
            }

            if (EpisodeInformation != null)
                EpisodeInformation.ConvertFromDatabase();
        }
    }
}
