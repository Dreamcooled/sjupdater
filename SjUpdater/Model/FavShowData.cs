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
using System.Xml.Serialization;
using SjUpdater.Utils;

namespace SjUpdater.Model
{
    public class FavShowData : PropertyChangedImpl
    {
        private string _name;
        private string _cover;
        private ObservableCollection<FavSeasonData> _seasons;
        private ShowData _show;
        private int _nrSeasons ;
        private int _nrEpisodes;
        private bool _newEpisodes;
        private bool _notified;
        private bool _isLoading;

        private UploadLanguage? _filterLanguage;
        private string _filterHoster;
        private bool? _filterShowNonSeason;
        private string _filterName;
        private string _filterFormat;
        private string _filterUploader;
        private string _filterSize;
        private string _filterRuntime;
        private bool? _filterShowNonEpisode;
        private string _infoUrl;
        private List<DownloadData> _allDownloads;

        public FavShowData(ShowData show, bool autofetch= false) :this()
        {
            _show = show;
            _name = show.Name;
            if (autofetch)
            {
                StaticInstance.SmartThreadPool.QueueWorkItem(Fetch);
            }
        }

        public FavShowData()
        {
            _seasons = new ObservableCollection<FavSeasonData>();
            _name = "";
            _cover = "";
            _seasons = new ObservableCollection<FavSeasonData>();
            _allDownloads = new List<DownloadData>();

            //the getters will return the default filter if the value is a null string
            _filterName = null;
            _filterHoster = null;
            _filterLanguage = null;
            _filterFormat = null;
            _filterUploader = null;
            _filterSize = null;
            _filterRuntime = null;
            _filterShowNonSeason = null;
            _filterShowNonEpisode = null;

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
            if (String.IsNullOrWhiteSpace(_infoUrl))
            {
                InfoUrl = SjInfo.SearchSjDe(Name);
            }
            IsLoading = true;
            String cover;
            var episodes = SjInfo.ParseSjOrgSite(_show, out cover,Settings.Instance.UploadCache);
            AllDownloads = episodes;
            if (cover != "")
            {
                Cover = cover;
            }
            _mutexFetch.ReleaseMutex();
            ApplyFilter(false);
            IsLoading = false;
        }




        public void ApplyFilter(bool reset=true)
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

            reset = reset || Seasons.Count == 0; //0 seasons is also considered as "reset".
            if(reset) //start from scratch?
                Seasons.Clear(); 

            UploadData currentUpload = null;
            bool ignoreCurrentUpload = false;
            foreach (var download in AllDownloads)
            {

                //Season stuff ------------------------------------------------------------------------------------
                if (currentSeasonData == null || currentSeasonData != download.Upload.Season)
                {
                    currentSeasonData = download.Upload.Season;
                    seasonNr = -1;  
                    Match m2 = new Regex("(?:season|staffel)\\s*(\\d+)", RegexOptions.IgnoreCase).Match(currentSeasonData.Title);
                    if (m2.Success)
                    {
                        int.TryParse(m2.Groups[1].Value,out seasonNr);
                    }
                }

                if (seasonNr == -1 && !FilterShowNonSeason.GetValueOrDefault()) //Filter: NonSeason Stuff
                {
                    continue;
                }

                if (currentFavSeason == null || currentFavSeason.Number != seasonNr)
                {
                    currentFavSeason = Seasons.FirstOrDefault(favSeasonData => favSeasonData.Number == seasonNr) ??
                                       new FavSeasonData() {Number = seasonNr,Show=this};
                }


                //upload stuff --------------------------------------------------------------------
                if (currentUpload == null || currentUpload != download.Upload)
                {
                    currentUpload = download.Upload;
                    ignoreCurrentUpload = true;
                    do
                    {
                        if ((currentUpload.Language & FilterLanguage) == 0) //Filter: Language
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

       


                int episodeNr = -1;
                if (seasonNr != -1)
                {
                    Match m1 = new Regex("S0{0,4}" + seasonNr + "E(\\d+)", RegexOptions.IgnoreCase).Match(download.Title);
                    if (m1.Success)
                    {
                        int.TryParse(m1.Groups[1].Value, out episodeNr);
                    }

                    if (episodeNr == -1 && !FilterShowNonEpisode.GetValueOrDefault()) //Filter: NonEpisode Stuff
                    {
                        continue;
                    }
                }

                //At This point we're sure we want the episode
                if (!Seasons.Contains(currentFavSeason)) //season not yet added?
                {
                    Seasons.Add(currentFavSeason);
                }

                FavEpisodeData currentFavEpisode = null;
              
                foreach (var episodeData in currentFavSeason.Episodes)
                {
                    if (seasonNr != -1)
                    {
                        if (episodeData.Number == episodeNr)
                        {
                            currentFavEpisode = episodeData;
                            break;
                        }
                    }
                    else
                    {
                        if (episodeData.Name == download.Title)
                        {
                            currentFavEpisode = episodeData;
                            break;
                        }
                    }
                }




                if (currentFavEpisode == null)
                {
                    currentFavEpisode = new FavEpisodeData();
                    currentFavEpisode.Season = currentFavSeason;
                    if (episodeNr == -1)
                    {
                        currentFavEpisode.Name = download.Title;
                        if (!reset)
                        {
                            currentFavEpisode.NewUpdate = true;
                            NewEpisodes = true;
                        }
                    }
                    else
                    {
                        currentFavEpisode.Number = episodeNr;
                        if (!reset)
                        {
                            currentFavEpisode.NewEpisode = true;
                            NewEpisodes = true;
                        }
                    }
                    currentFavSeason.Episodes.Add(currentFavEpisode);
                    if (!String.IsNullOrWhiteSpace(InfoUrl))
                    {
                        Task.Run(delegate
                        {
                            currentFavEpisode.ReviewInfoReview = SjInfo.ParseSjDeSite(InfoUrl,
                                currentFavEpisode.Season.Number, currentFavEpisode.Number);
                        });
                    }
                    currentFavEpisode.Downloads.Add(download);
                }
                else
                {
                    if (currentFavEpisode.Downloads.All(d => d.Title != download.Title))
                    {
                        currentFavEpisode.Downloads.Add(download);
                        if (!reset)
                        {
                            currentFavEpisode.NewUpdate = true;
                        }
                    }

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

        public String InfoUrl
        {
            get { return _infoUrl; }
            set
            {
                if (value == _infoUrl)
                    return;
                _infoUrl = value;
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
                if (season.Number != -1)
                {
                    seasons++;
                    episodes += season.NumberOfEpisodes;
                }
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

        public bool? FilterShowNonSeason
        {
            get
            {
                if (!_filterShowNonSeason.HasValue)
                    return Settings.Instance.FilterShowNonSeason;
                return _filterShowNonSeason;
            }
            set
            {
                if (value == _filterShowNonSeason) return;
                _filterShowNonSeason = value;
                OnPropertyChanged();
            }
        }

        public bool? FilterShowNonEpisode
        {
            get
            {
                if (!_filterShowNonEpisode.HasValue)
                    return Settings.Instance.FilterShowNonEpisode;
                return _filterShowNonEpisode;
            }
            set
            {
                if (value == _filterShowNonEpisode) return;
                _filterShowNonEpisode = value;
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


    }
}
