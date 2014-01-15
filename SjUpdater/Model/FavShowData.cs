using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        private UploadLanguage _filterLanguage;
        private string _filterHoster;
        private bool _filterShowNonSeason;
        private string _filterFormat;
        private string _filterUploader;
        private string _filterSize;
        private string _filterRuntime;
        private bool _filterShowNonEpisode;
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

            _filterHoster = "";
            _filterLanguage = UploadLanguage.Both;
            _filterFormat = "";
            _filterUploader = "";
            _filterSize = "";
            _filterRuntime = "";
            _filterShowNonSeason = true;
            _filterShowNonEpisode = true;


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

            String cover;
            var episodes = SjInfo.ParseSjOrgSite(_show, out cover);
            AllDownloads = episodes;
            if (cover != "")
            {
                Cover = cover;
            }
            _mutexFetch.ReleaseMutex();
            ApplyFilter(false);

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

            if(reset) //start from scratch?
                Seasons.Clear(); 

            UploadData currentUpload = null;
            bool ignoreCurrentUpload = false;
            foreach (var episode in AllDownloads)
            {

                //Season stuff ------------------------------------------------------------------------------------
                if (currentSeasonData == null || currentSeasonData != episode.Upload.Season)
                {
                    currentSeasonData = episode.Upload.Season;
                    seasonNr = -1;
                    Match m2 = new Regex("(?:season|staffel)\\s*(\\d+)", RegexOptions.IgnoreCase).Match(currentSeasonData.Title);
                    if (m2.Success)
                    {
                        int.TryParse(m2.Groups[1].Value,out seasonNr);
                    }
                }

                if (seasonNr == -1 && !FilterShowNonSeason) //Filter: NonSeason Stuff
                {
                    continue;
                }

                if (currentFavSeason == null || currentFavSeason.Number != seasonNr)
                {
                    currentFavSeason = Seasons.FirstOrDefault(favSeasonData => favSeasonData.Number == seasonNr);
                    if (currentFavSeason == null)
                    {
                        currentFavSeason = new FavSeasonData() {Number = seasonNr,Show=this};
                    }
                }


                //upload stuff --------------------------------------------------------------------
                if (currentUpload == null || currentUpload != episode.Upload)
                {
                    currentUpload = episode.Upload;
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
                if (!String.IsNullOrWhiteSpace(FilterHoster))
                {
                    var r = new Regex(FilterHoster);
                    var dls = episode.Links.Keys.Where(hoster => r.Match(hoster).Success).ToList(); //all keys that match the regex
                    if (!dls.Any()) //Filter: Hoster
                        continue;
                    for (int i = episode.Links.Keys.Count - 1; i >= 0; i--)
                    {
                        string key = episode.Links.Keys.ElementAt(i);
                        if (!dls.Contains(key))
                        {
                            episode.Links.Remove(key);
                        }
                    }
                }


                int episodeNr = -1;
                if (seasonNr != -1)
                {
                    Match m1 = new Regex("S0{0,4}" + seasonNr + "E(\\d+)", RegexOptions.IgnoreCase).Match(episode.Title);
                    if (m1.Success)
                    {
                        int.TryParse(m1.Groups[1].Value, out episodeNr);
                    }

                    if (episodeNr == -1 && !FilterShowNonEpisode) //Filter: NonEpisode Stuff
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
                        if (episodeData.Name == episode.Title)
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
                        currentFavEpisode.Name = episode.Title;
                    }
                    else
                    {
                        currentFavEpisode.Number = episodeNr;
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
                    currentFavEpisode.Downloads.Add(episode);
                }
                else
                {
                    if (currentFavEpisode.Downloads.All(download => download.Title != episode.Title))
                    {
                        currentFavEpisode.Downloads.Add(episode);
                    }
                }
                
                
              

            }

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
        public List<DownloadData> AllDownloads
        {
            get { return _allDownloads; }
            set
            {
                _allDownloads = value; 
                OnPropertyChanged();
            }
        }

        public UploadLanguage FilterLanguage
        {
            get { return _filterLanguage; }
            set
            {
                if (value == _filterLanguage)
                    return;
                _filterLanguage = value;
                OnPropertyChanged();
            }
        }

        public String FilterHoster
        {
            get { return _filterHoster; }
            set
            {
                if (value == _filterHoster) return;
                _filterHoster = value;
                OnPropertyChanged();
            }
        }

        public bool FilterShowNonSeason
        {
            get { return _filterShowNonSeason; }
            set
            {
                if (value == _filterShowNonSeason) return;
                _filterShowNonSeason = value;
                OnPropertyChanged();
            }
        }

        public bool FilterShowNonEpisode
        {
            get { return _filterShowNonEpisode; }
            set
            {
                if (value == _filterShowNonEpisode) return;
                _filterShowNonEpisode = value;
                OnPropertyChanged();
            }
        }

        public String FilterFormat
        {
            get { return _filterFormat; }
            set
            {
                if (value == _filterFormat) return;
                _filterFormat = value;
                OnPropertyChanged();
            }
        }

        public String FilterUploader
        {
            get { return _filterUploader; }
            set
            {
                if (value == _filterUploader) return;
                _filterUploader = value;
                OnPropertyChanged();
            }
        }

        public String FilterSize
        {
            get { return _filterSize; }
            set
            {
                if (value == _filterSize) return;
                _filterSize = value;
                OnPropertyChanged();
            }
        }

        public String FilterRuntime
        {
            get { return _filterRuntime; }
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
                OnPropertyChanged();
            }
        }


    }
}
