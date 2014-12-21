using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media.Animation;
using System.Xml.Serialization;
using Amib.Threading;
using MahApps.Metro.Converters;
using SjUpdater.Provider;
using SjUpdater.Utils;

namespace SjUpdater.Model
{
    public class FavShowData : PropertyChangedImpl
    {
        private string _name;
        private string _cover;
        private ObservableCollection<FavSeasonData> _seasons;
        private ObservableCollection<DownloadData> _nonSeasons; 
        private ShowData _show;
        private int _nrSeasons ;
        private int _nrEpisodes;
        private bool _newEpisodes;
        private bool _notified;
        private bool _isLoading;
        private bool _resetOnRefresh;

        private UploadLanguage? _filterLanguage;
        private string _filterHoster;
        private string _filterName;
        private string _filterFormat;
        private string _filterUploader;
        private string _filterSize;
        private string _filterRuntime;

        private object _providerData;
        //private ShowInformation _showInformation;

        private List<DownloadData> _allDownloads;
        private readonly bool _isNewShow; //=false

        public FavShowData(ShowData show, bool autofetch= false) :this()
        {
            _show = show;
            _isNewShow = true;
            _name = show.Name;
            if (autofetch)
            {
                StaticInstance.ThreadPool.QueueWorkItem(Fetch,true, ThreadPriority.BelowNormal);
            }
        }

        public FavShowData()
        {
            _seasons = new ObservableCollection<FavSeasonData>();
            _name = "";
            _cover = "";
            _nonSeasons = new ObservableCollection<DownloadData>();
            _allDownloads = new List<DownloadData>();
            _providerData = null;
           // _showInformation = null; //TODO: fill & use this info

            //the getters will return the default filter if the value is a null string
            _filterName = null;
            _filterHoster = null;
            _filterLanguage = null;
            _filterFormat = null;
            _filterUploader = null;
            _filterSize = null;
            _filterRuntime = null;

        }

        public void SetResetOnRefresh()
        {
            _resetOnRefresh = true;
        }

        readonly Mutex _mutexFetch = new Mutex();
        readonly Mutex _mutexFilter = new Mutex();
        public void Fetch()
        {
            if (!_mutexFetch.WaitOne(0)) //try get mutex
            {
                //already a fetch in progress
                _mutexFetch.WaitOne(); //wait on finish
                _mutexFetch.ReleaseMutex();
                return;
            }
            if (_providerData==null)
            {
               // InfoUrl = SjInfo.SearchSjDe(Name);
                ProviderData = ProviderManager.GetProvider().FindShow(Name);
            }

            try
            {
                IsLoading = true;
                String cover;
                var episodes = SjInfo.ParseSjOrgSite(_show, out cover, Settings.Instance.UploadCache);
                AllDownloads = episodes;
                if (cover != "")
                {
                    Cover = cover;
                }
                _mutexFetch.ReleaseMutex();
                if (_resetOnRefresh)
                {
                    _resetOnRefresh = false;
                    ApplyFilter(true); 
                }
                else
                {
                    ApplyFilter(false);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void ApplyFilter(bool reset,bool notifications=true)
        {
            if (AllDownloads == null || !AllDownloads.Any())
            {
                Fetch();
                return;
            }

            _mutexFilter.WaitOne();

            FavSeasonData currentFavSeason = null;
            SeasonData currentSeasonData = null;
            int seasonNr = -1;

            ObservableCollection<FavSeasonData> newSeasons = new ObservableCollection<FavSeasonData>();
            ObservableCollection<DownloadData> newNonSeasons = new ObservableCollection<DownloadData>();

            if (_isNewShow) notifications = false;
            reset = reset || _isNewShow;
            if (!reset)
            {
                newSeasons = Seasons;
                newNonSeasons = NonSeasons;
            }

            UploadData currentUpload = null;
            bool ignoreCurrentUpload = false;
            foreach (var download in AllDownloads)
            {
                //upload stuff --------------------------------------------------------------------
                if (currentUpload == null || currentUpload != download.Upload)
                {
                    currentUpload = download.Upload;
                    ignoreCurrentUpload = true;
                    do
                    {

                        UploadLanguage language = currentUpload.Language;
                        if (!Settings.Instance.MarkSubbedAsGerman && currentUpload.Subbed) //dont mark german-subbed as german
                        {
                            language&=~UploadLanguage.German; //remove german
                        }

                        if ((language & FilterLanguage) == 0) //Filter: Language
                            break;

                        if (!String.IsNullOrWhiteSpace(FilterRuntime) &&     //Filter: Runtime
                            !(new Regex(FilterRuntime).Match(currentUpload.Runtime).Success))
                            break;
                        

                        if (!String.IsNullOrWhiteSpace(FilterSize) &&     //Filter: Size
                            !(new Regex(FilterSize).Match(currentUpload.Size).Success))
                            break;
                        
                        if (!String.IsNullOrWhiteSpace(FilterUploader) &&     //Filter: Uploader
                            !(new Regex(FilterUploader).Match(currentUpload.Uploader).Success))
                            break;
                        
                        if (!String.IsNullOrWhiteSpace(FilterFormat) &&     //Filter: Format
                            !(new Regex(FilterFormat).Match(currentUpload.Format).Success))
                            break;
                       
                        ignoreCurrentUpload = false;

                    } while (false);
                }

                if (ignoreCurrentUpload) //Filter: All upload stuff
                {
                    continue;
                }

                //episode stuff ---------------------

                if (!String.IsNullOrWhiteSpace(FilterName) && //Filter: Name
                    !(new Regex(FilterName).Match(download.Title).Success))
                    continue;

                if (!String.IsNullOrWhiteSpace(FilterHoster))
                {
                    var r = new Regex(FilterHoster);
                    var dls = download.Links.Keys.Where(hoster => r.Match(hoster).Success).ToList(); //all keys that match the regex
                    if (!dls.Any()) //Filter: Hoster
                        continue;
                    for (int i = download.Links.Keys.Count - 1; i >= 0; i--)
                    {
                        string key = download.Links.Keys.ElementAt(i);
                        if (!dls.Contains(key))
                        {
                            download.Links.Remove(key);
                        }
                    }
                }

                //------------------------------------------

                //Season stuff ------------------------------------------------------------------------------------
                if (currentSeasonData == null || currentSeasonData != download.Upload.Season)
                {
                    currentSeasonData = download.Upload.Season;
                    seasonNr = -1;
                    Match m2 = new Regex("(?:season|staffel)\\s*(\\d+)", RegexOptions.IgnoreCase).Match(currentSeasonData.Title);
                    if (m2.Success)
                    {
                        int.TryParse(m2.Groups[1].Value, out seasonNr);
                    }
                }

                if (seasonNr == -1)
                {
                    if (newNonSeasons.All(d => d.Title != download.Title))
                    {
                        newNonSeasons.Add(download);
                    }
                    continue;
                }

                if (currentFavSeason == null || currentFavSeason.Number != seasonNr)
                {
                    currentFavSeason = newSeasons.FirstOrDefault(favSeasonData => favSeasonData.Number == seasonNr) ??
                                       new FavSeasonData() { Number = seasonNr, Show = this };

                    if (!newSeasons.Contains(currentFavSeason)) //season not yet added?
                    {
                        newSeasons.Add(currentFavSeason);
                    }
                }

                int episodeNr = -1;
            
                MatchCollection mts = new Regex("S0{0,4}" + seasonNr + "E(\\d+)", RegexOptions.IgnoreCase).Matches(download.Title);
                MatchCollection mts_ep = new Regex("[^A-Z]E(\\d+)", RegexOptions.IgnoreCase).Matches(download.Title);
                if (mts.Count==1 && mts_ep.Count==1) //if there is exactly one match for "S<xx>E<yy>" and there is no second "E<zz>" (e.g. S01E01-E12) 
                {
                    int.TryParse(mts[0].Groups[1].Value, out episodeNr);
                }
                

                if (episodeNr == -1)
                {
                    if (currentFavSeason.NonEpisodes.All(d => d.Title != download.Title))
                    {
                        currentFavSeason.NonEpisodes.Add(download);
                    }
                    continue;
                }

                FavEpisodeData currentFavEpisode = currentFavSeason.Episodes.FirstOrDefault(episodeData => episodeData.Number == episodeNr);

                if (currentFavEpisode == null)
                {
                    currentFavEpisode = new FavEpisodeData();
                    currentFavEpisode.Season = currentFavSeason;
                    currentFavEpisode.Number = episodeNr;
                    bool existed = false;

                    var oldSeason = Seasons.FirstOrDefault(s => s.Number == currentFavSeason.Number);
                    if (oldSeason != null)
                    {
                        var oldEpisode = oldSeason.Episodes.FirstOrDefault(e => e.Number == currentFavEpisode.Number);
                        if (oldEpisode != null) //we can copy old data to current episode
                        {
                            currentFavEpisode.Watched = oldEpisode.Watched;
                            currentFavEpisode.Downloaded = oldEpisode.Downloaded;
                            currentFavEpisode.EpisodeInformation = oldEpisode.EpisodeInformation;
                            existed = true;
                        }
                    }

                    if (notifications && !existed) {
                        currentFavEpisode.NewEpisode = true;
                        NewEpisodes = true;
                    }
            
                    currentFavSeason.Episodes.Add(currentFavEpisode);

                    currentFavEpisode.Downloads.Add(download);

                    if (ProviderData != null && (currentFavEpisode.EpisodeInformation == null || reset))
                    {
                        StaticInstance.ThreadPool.QueueWorkItem(() =>
                        {
                            //currentFavEpisode.ReviewInfoReview = SjInfo.ParseSjDeSite(InfoUrl, currentFavEpisode.Season.Number, currentFavEpisode.Number);
                            currentFavEpisode.EpisodeInformation = ProviderManager.GetProvider().GetEpisodeInformation(ProviderData, currentFavEpisode.Season.Number, currentFavEpisode.Number);
                        });
                    }
                }
                else
                {
                    FavEpisodeData oldEpisode = null;
                    var oldSeason = Seasons.FirstOrDefault(s => s.Number == currentFavSeason.Number);
                    if (oldSeason != null)
                    {
                        oldEpisode = oldSeason.Episodes.FirstOrDefault(e => e.Number == currentFavEpisode.Number);
                    }
                    
                    if (currentFavEpisode.Downloads.All(d => d.Title != download.Title))
                    {
                        currentFavEpisode.Downloads.Add(download);
                        if (notifications && (oldEpisode==null ||  oldEpisode.Downloads.All(d => d.Title != download.Title)))
                        {
                            currentFavEpisode.NewUpdate = true;
                        }
                    }
                }  
            }

            if (reset)
            {
                Seasons.Clear();
                foreach (var season in newSeasons)
                {
                    Seasons.Add(season);
                }
                NonSeasons.Clear();
                foreach (var nonSeason in newNonSeasons)
                {
                    NonSeasons.Add(nonSeason);
                }
            }


            RecalcNumbers();
            _mutexFilter.ReleaseMutex();

        }

        public ShowData Show
        {
            get { return _show; }
            set
            {
                _show = value;
                OnPropertyChanged();
            }
        }

        public String Name
        {
            get { return _name; }
            set
            {
                if (value == _name)
                    return;
                _name = value;
                OnPropertyChanged();
            }
        }

        public String Cover
        {
            get { return _cover; }
            set
            {
                if (value == _cover)
                    return;
                _cover = value;
                OnPropertyChanged();
            }
        }

        public object ProviderData
        {
            get { return _providerData; }
            set
            {
                if (value == _providerData)
                    return;
                _providerData = value;
                OnPropertyChanged();
                
            }
        }

        [XmlIgnore]
        public bool IsLoading
        {
            get { return _isLoading; }
            internal set
            {
                _isLoading = value;
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

        /// <summary>
        /// Is set to true when we have new episodes (count > cached number). Reset this to false, yourself
        /// </summary>
        [XmlIgnore]
        public bool NewEpisodes
        {
            get { return _newEpisodes; }
            internal set
            {
                if (value == _newEpisodes) return;
                _newEpisodes = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Not touched by class at all. It's intended to be set to true when you have notified the user about updates.
        /// </summary>
        [XmlIgnore]
        public bool Notified
        {
            get { return _notified; }
            internal set
            {
                if (value == _notified) return;
                _notified = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        public int NumberOfSeasons
        {
            get { return _nrSeasons; }
            internal set
            {
                if (value == _nrSeasons) return;
                _nrSeasons = value;
                OnPropertyChanged();
            }
        }

        private void RecalcNumbers()
        {
            int episodes = 0;
            int seasons = 0;
            foreach (FavSeasonData season in _seasons)
            {
                    seasons++;
                    episodes += season.NumberOfEpisodes;
            }
            NumberOfEpisodes = episodes;
            NumberOfSeasons = seasons;
        }

        [XmlIgnore]
        public List<DownloadData> AllDownloads
        {
            get { return _allDownloads; }
            set
            {
                _allDownloads = value; 
                OnPropertyChanged();
            }
        }

        public UploadLanguage? FilterLanguage
        {
            get
            {
                if (!_filterLanguage.HasValue)
                    return Settings.Instance.FilterLanguage;
                return _filterLanguage;
            }
            set
            {
                if (value == _filterLanguage)
                    return;
                _filterLanguage = value;
                OnPropertyChanged();
            }
        }

        public String FilterName
        {
            get
            {
                if(_filterName==null)
                    return Settings.Instance.FilterName;
                return _filterName;
            }
            set
            {
                if (value == _filterName) return;
                _filterName = value;
                OnPropertyChanged();
            }
        }

        public String FilterHoster
        {
            get
            {
                if (_filterHoster == null)
                    return Settings.Instance.FilterHoster;
                return _filterHoster;
            }
            set
            {
                if (value == _filterHoster) return;
                _filterHoster = value;
                OnPropertyChanged();
            }
        }

        public String FilterFormat
        {
            get
            {
                if (_filterFormat == null)
                    return Settings.Instance.FilterFormat; 
                return _filterFormat;
            }
            set
            {
                if (value == _filterFormat) return;
                _filterFormat = value;
                OnPropertyChanged();
            }
        }

        public String FilterUploader
        {
            get
            {
                if (_filterUploader == null)
                    return Settings.Instance.FilterUploader; 
                return _filterUploader;
            }
            set
            {
                if (value == _filterUploader) return;
                _filterUploader = value;
                OnPropertyChanged();
            }
        }

        public String FilterSize
        {
            get
            {
                if (_filterSize == null)
                    return Settings.Instance.FilterSize; 
                return _filterSize;
            }
            set
            {
                if (value == _filterSize) return;
                _filterSize = value;
                OnPropertyChanged();
            }
        }

        public String FilterRuntime
        {
            get
            {
                if (_filterRuntime == null)
                    return Settings.Instance.FilterRuntime; 
                return _filterRuntime;
            }
            set
            {
                if (value == _filterRuntime) return;
                _filterRuntime = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<FavSeasonData> Seasons
        {
            get { return _seasons; }
            internal set
            {
                _seasons = value;
                RecalcNumbers();
                OnPropertyChanged();
            }
        }
        public ObservableCollection<DownloadData> NonSeasons
        {
            get { return _nonSeasons; }
            internal set
            {
                _nonSeasons = value;
                RecalcNumbers();
                OnPropertyChanged();
            }
        }
    }
}
