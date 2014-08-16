using System;
using System.Collections.ObjectModel;
using System.IO;
using SjUpdater.Model;
using SjUpdater.Utils;
using SjUpdater.XML;

namespace SjUpdater
{
    public class Settings
    {

        #region Static Stuff

        private static readonly Settings setti;

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


        /// <summary>
        /// The TV-Show Cache
        /// </summary>
        public ObservableCollection<FavShowData> TvShows { get; set; } 

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
        /// How often to update the TV Shows (in miliseconds)
        /// </summary>
        public int UpdateTime { get; set; }

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
            NumFetchThreads = 3;
            ThemeAccent = "Green";
            ThemeBase = "BaseDark";
            UpdateTime = 1000*60*15; //15min
            FilterLanguage = UploadLanguage.Any;
            FilterShowNonEpisode = true;
            FilterShowNonSeason = true;
            NoPersonalData = false;
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