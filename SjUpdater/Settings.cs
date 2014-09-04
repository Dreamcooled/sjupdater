using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using SjUpdater.Model;
using SjUpdater.Utils;
using SjUpdater.XML;

namespace SjUpdater
{
    public class Settings
    {

        #region Static Stuff

        private static readonly Settings setti;
        private ObservableCollection<FavShowData> _tvShows;

        static Settings()
        {
            if (File.Exists("config.xml"))
            {
                setti = Load("config.xml");
            }
            else
            {
                setti = new Settings();
                setti.Save("config.xml");
            }

        }

        public static void Save()
        {
            setti.Save("config.xml");
        }

        public static Settings Instance
        {
            get
            {
                return setti;
            }
        } 
        #endregion

        private readonly UploadCache uploadCache = new UploadCache();
        [XmlIgnore]
        public UploadCache UploadCache
        {
            get { return uploadCache; }
        }

        /// <summary>
        /// The TV-Show Cache
        /// </summary>
        public ObservableCollection<FavShowData> TvShows
        {
            get { return _tvShows; }
            set
            {
                foreach (var favShowData in value)
                {
                    foreach (var favSeasonData in favShowData.Seasons)
                    {
                        foreach (var favEpisodeData in favSeasonData.Episodes)
                        {
                            foreach (var downloadData in favEpisodeData.Downloads)
                            {
                                if(downloadData.Upload==null) continue;
                                UploadData v = uploadCache.GetUniqueUploadData(downloadData.Upload);
                                if (ReferenceEquals(v, downloadData.Upload))
                                {
                                    //cache hit or new to cache
                                }
                                else
                                {
                                    downloadData.Upload = v; //correct, to use value from cache
                                }
                            }
                        }
                    }
                }
   
                _tvShows = value;
            }
        }

        /// <summary>
        /// Wheather to sort the Seasons inside a Show asc or desc
        /// </summary>
        public bool SortSeasonsDesc { get; set; }

        /// <summary>
        /// Wheather to sort the Episodes inside a Season asc or desc
        /// </summary>
        public bool SortEpisodesDesc { get; set; }

        /// <summary>
        /// Wheather to sort the Shows alphabetically or by new/old
        /// </summary>
        public bool SortShowsAlphabetically { get; set; }

        /// <summary>
        /// The Numer of Threads used to fetch updates on programm start
        /// </summary>
        public uint NumFetchThreads { get; set; }

        /// <summary>
        /// Whether to minimize only to tray when pressing the close button
        /// </summary>
        public bool MinimizeToTray { get; set; }

        /// <summary>
        /// How often to update the TV Shows (in milliseconds)
        /// </summary>
        public int UpdateTime { get; set; }

        public bool ShowNotifications { get; set; }

        /// <summary>
        /// How long the popup will stay, use 0 to not automatically close (in milliseconds)
        /// </summary>
        public int NotificationTimeout { get; set; }

        public bool EnableImages { get; set; }

        /// <summary>
        /// whether it should automatically check for updates
        /// </summary>
        public bool CheckForUpdates { get; set; }

        /// <summary>
        /// Theme Color
        /// </summary>
        public String ThemeAccent { get; set; }

        /// <summary>
        /// Theme Base
        /// </summary>
        public String ThemeBase  { get; set; }

        /// <summary>
        /// Whether we are allowed to send personal data to stats server
        /// </summary>
        public bool NoPersonalData { get; set; }

        //Default Filters: See FavShowData.cs

        public UploadLanguage FilterLanguage { get; set; }
        public String FilterName{ get; set; }
        public String FilterHoster { get; set; }
        public bool FilterShowNonSeason { get; set; }
        public bool FilterShowNonEpisode { get; set; }
        public String FilterFormat { get; set; }
        public String FilterUploader { get; set; }
        public String FilterSize { get; set; }
        public String FilterRuntime { get; set; }

        public Settings()
        {
            TvShows = new ObservableCollection<FavShowData>();
            SortSeasonsDesc = false;
            SortEpisodesDesc = false;
            NumFetchThreads = 5;
            ThemeAccent = "Green";
            ThemeBase = "BaseDark";
            UpdateTime = 1000*60*15; //15min
            ShowNotifications = true;
            NotificationTimeout = 10000; //10 seconds
            FilterLanguage = UploadLanguage.Any;
            FilterShowNonEpisode = true;
            FilterShowNonSeason = true;
            NoPersonalData = false;
            EnableImages = true;
            CheckForUpdates = true;
        }
        
        public static Settings Load(string filename)
        {

            return XmlSerialization.LoadFromXml<Settings>(filename);
        }

        public void Save(string filename)
        {
            XmlSerialization.SaveToXml(this,filename);
        }
    }
}