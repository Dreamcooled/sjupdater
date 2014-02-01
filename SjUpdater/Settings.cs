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
        /// The Numer of Threads used to fetch updates on programm start
        /// </summary>
        public uint NumFetchThreads { get; set; }

        
        public Settings()
        {
            TvShows = new ObservableCollection<FavShowData>();
            SortSeasonsDesc = false;
            SortEpisodesDesc = false;
            NumFetchThreads = 3;
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