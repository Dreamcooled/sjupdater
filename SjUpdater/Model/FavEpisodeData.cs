using System;
using System.Collections.ObjectModel;
using SjUpdater.Utils;

namespace SjUpdater.Model
{
    public class FavEpisodeData : PropertyChangedImpl
    {
        private string _name;
        private int _number;
        private ObservableCollection<DownloadData> _downloads;
        private FavSeasonData _season;
        private SjDeReview _reviewInfoReview;

        public FavEpisodeData()
        {
            _downloads = new ObservableCollection<DownloadData>();
            _name = "";
            _number = -1;
            _reviewInfoReview = null;
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
