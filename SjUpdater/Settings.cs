using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;
using SjUpdater.Model;
using SjUpdater.Utils;
using SjUpdater.XML;

namespace SjUpdater
{
    public class Settings : Database.IDatabaseCompatibility
    {

        #region Static Stuff

        private static readonly Settings setti;
        private ObservableCollection<FavShowData> _tvShows;
        private const string CONFIG = "config.xml";
        private const string CONFIGBAK = "config.xml.autobackup";
        private const string DBBAKEXTENSION = ".autobackup";


        static Settings()
        {
            if (File.Exists(CONFIG))
            {
                bool overwrite =false;

                try
                {
                    setti = Load(CONFIG, out overwrite);
                }
                catch (Exception ex) //eror while loading config (file broken?)
                {
                    if (File.Exists(CONFIGBAK)) //There's a backup, hurray!
                    {
                        try
                        {
                            setti = Load(CONFIGBAK, out overwrite); //try to load backup
                            overwrite = true; //Overwrite broken config anyway (not only if updated)
                            MessageBox.Show(
                                "SjUpdater was not terminated properly ("+CONFIG+" broken). Old Settings restored from backup ("+CONFIGBAK+"). " +
                                "Submit a Bugreport if you see this message often. Details:\n"+ ex,
                                "SjUpdater was not terminated properly");
                        }
                        catch (Exception ex2) //Loading backup failed as well :(
                        {
                            MessageBox.Show(
                             "SjUpdater was not terminated properly (" + CONFIG + " broken). Backup (" + CONFIGBAK + ") couldn't be restored either. " +
                             "Your config was deleted and you have to start over again :( . " +
                             "Report the following to the developer:\n" + ex2,
                             "SjUpdater was not terminated properly");
                        }
                    }
                    else //No Backup available :(
                    {
                        MessageBox.Show(
                               "SjUpdater was not terminated properly (" + CONFIG + " broken) and " +
                               "there was no backup (" + CONFIGBAK + ") for your config available. " +
                               "Your config was deleted and you have to start over again :( . " +
                               "Submit a Bugreport if you see this message often. Details:\n" + ex,
                               "SjUpdater was not terminated properly");
                    }
                }

                if (setti != null) //loading ok
                {
                    if (overwrite) setti.Save(CONFIG); //if we need to save the changes because of a settings migration
                    File.Copy(CONFIG, CONFIGBAK, true); //backup the successfully loadable file.
                }
            }

            if (setti==null) //either the user has started the application for the first time or the config could not be loaded
            {
                setti = new Settings(); //Create a new settings instance/file
                setti.Save(CONFIG); //and save it
                File.Copy(CONFIG, CONFIGBAK, true); //backup the new file (paranoid)
            }

            string dbPath = Database.DatabaseWriter.GetDBPath(); // Load database - Calvin 12-Feb-2016
            string dbBakPath = dbPath + DBBAKEXTENSION;
            if (File.Exists(dbPath))
            {
                try
                {
                    Database.DatabaseWriter.LoadFromDatabase(setti);
                }
                catch (Exception ex) //eror while loading config (file broken?)
                {
                    if (File.Exists(dbBakPath)) //There's a backup, hurray!
                    {
                        try
                        {
                            File.Copy(dbPath, dbBakPath, true);
                            Database.DatabaseWriter.LoadFromDatabase(setti); //try to load backup
                            MessageBox.Show(
                                "SjUpdater was not terminated properly (" + dbPath + " broken). Old database restored from backup (" + dbBakPath + "). " +
                                "Submit a Bugreport if you see this message often. Details:\n" + ex,
                                "SjUpdater was not terminated properly");
                        }
                        catch (Exception ex2) //Loading backup failed as well :(
                        {
                            MessageBox.Show(
                             "SjUpdater was not terminated properly (" + dbPath + " broken). Backup (" + dbBakPath + ") couldn't be restored either. " +
                             "Your database was deleted and you have to start over again :( . " +
                             "Report the following to the developer:\n" + ex2,
                             "SjUpdater was not terminated properly");
                        }
                    }
                    else //No Backup available :(
                    {
                        MessageBox.Show(
                               "SjUpdater was not terminated properly (" + dbPath + " broken) and " +
                               "there was no backup (" + dbBakPath + ") for your dbBakPath available. " +
                               "Your config was deleted and you have to start over again :( . " +
                               "Submit a Bugreport if you see this message often. Details:\n" + ex,
                               "SjUpdater was not terminated properly");
                    }
                }

                if (setti != null) //loading ok
                {
                    if (File.Exists(dbPath))
                        File.Copy(dbPath, dbPath + DBBAKEXTENSION, true);
                }
            }
        }

        public static void Save()
        {
            setti.Save(CONFIG);
        }

        public static Settings Instance
        {
            get
            {
                return setti;
            }
        } 
        #endregion

        private const int SettingsVersion = 2;

        private readonly UploadCache uploadCache = new UploadCache();
        [XmlIgnore]
        private uint numFetchThreads;

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
        /// The Numer of Threads used to fetch updates on programm start
        /// </summary>

        public bool MarkSubbedAsGerman { get; set; }
        public uint NumFetchThreads 
        { 
            get { return numFetchThreads; }
            set
            {
                numFetchThreads = value;
                StaticInstance.ThreadPool.MaxThreads = value > 12 ? 12 : (int) value;
            } 
        }

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



        /// <summary>
        /// Whether or not Episodes can be favorized
        /// </summary>
        public bool UseFavorites { get; set; }


        //Default Filters: See FavShowData.cs

        public UploadLanguage FilterLanguage { get; set; }
        public String FilterName{ get; set; }
        public String FilterHoster { get; set; }
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
            MarkSubbedAsGerman = false;
            NoPersonalData = false;
            EnableImages = true;
            CheckForUpdates = true;
            UseFavorites = true;
        }
        
        public static Settings Load(string filename, out bool converted)
        {
            converted = false;
            int actualVersion;
            Settings s = XmlSerialization.LoadFromXml<Settings>(filename, SettingsVersion,out actualVersion);

            if (s == null) //version to new
            {
                converted = true;
                Debug.Assert(actualVersion > SettingsVersion);
                File.Copy(filename,filename+".v"+actualVersion,true);
                return null;
            }
            if (actualVersion < SettingsVersion) //import old version
            {
                converted = true;
                File.Copy(filename, filename + ".v" + actualVersion,true);
                return Import(s, actualVersion, SettingsVersion);
            }
            return s;
        }

        private static Settings Import(Settings actualSettings, int actualVersion, int targetVersion)
        {
            if (actualVersion > targetVersion) return null;
            if ((targetVersion - actualVersion) > 1)
            {
                Settings s1 = Import(actualSettings, actualVersion, actualVersion + 1);
                return Import(s1, actualVersion + 1, targetVersion);
            }

            switch (actualVersion)
            {
                case 1: //upgrading from v1 to v2

                    foreach (var favShow in actualSettings.TvShows)
                    {
                        for (int i = favShow.Seasons.Count-1; i>=0; i--)
                        {
                            var favSeason = favShow.Seasons[i];
                            if (favSeason.Number == -1)
                            {
                                foreach (var downloadData in favSeason.Episodes.SelectMany(favEpisode => favEpisode.Downloads))
                                {
                                    favShow.NonSeasons.Add(downloadData);
                                }
                                favShow.Seasons.RemoveAt(i);
                            }
                            else
                            {
                                var nonEpisode = favSeason.Episodes.FirstOrDefault(favEpisode => favEpisode.Number == -1);
                                if (nonEpisode != null)
                                {
                                    foreach (var downloadData in nonEpisode.Downloads)
                                    {
                                        favSeason.NonEpisodes.Add(downloadData);
                                    }
                                    favSeason.Episodes.Remove(nonEpisode);
                                }  
                            }
                        }


                        favShow.SetResetOnRefresh();
                    }
                    break;

                   
            }

            return actualSettings;

        }

        public void Save(string filename)
        {
            bool dbSaved = false;
            try
            {
                Database.DatabaseWriter.SaveToDatabase(this);
                dbSaved = true;
            }
            catch (Exception ex)
            {
                // Problem saving to DB, delete broken DB if it exists and save to XML instead - Calvin 12-Feb-2016

                string dbPath = Database.DatabaseWriter.GetDBPath();
                MessageBox.Show(
                    "Could not save shows to database (" + dbPath + "), saving to XML file (" + CONFIG + ") instead. " +
                    "Submit a Bugreport if you see this message often. Details:\n" + ex,
                    "SjUpdater was not terminated properly");

                if (File.Exists(dbPath))
                    File.Delete(dbPath);
            }

            if (dbSaved)
            {
                ObservableCollection<FavShowData> temp = TvShows;
                TvShows = new ObservableCollection<FavShowData>(); // Skip writing shows to xml since they're already written to database - Calvin 12-Feb-2016

                XmlSerialization.SaveToXml(this, filename, SettingsVersion);
                TvShows = temp;
            }
            else
                XmlSerialization.SaveToXml(this,filename,SettingsVersion);
        }

        public void ConvertToDatabase()
        {
            foreach (FavShowData tvShow in TvShows)
            {
                tvShow.ConvertToDatabase();
            }
        }

        public void ConvertFromDatabase()
        {
            foreach (FavShowData tvShow in TvShows)
            {
                tvShow.ConvertFromDatabase();
            }
        }
    }
}