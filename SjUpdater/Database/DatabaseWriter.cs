using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using SjUpdater.Model;

namespace SjUpdater.Database
{
    class DatabaseWriter
    {
        public static string GetDBPath()
        {
            return ConfigurationManager.ConnectionStrings["SjUpdater.Database.CustomDbContext"].ConnectionString.Split('=')[1];
        }

        public static void PreWrite(Settings settings)
        {

        }

        public static void SaveToDatabase(Settings settings)
        {
            try
            {
                using (CustomDbContext db = new CustomDbContext())
                {
                    db.Database.Delete();

                    db.Database.Create();

                    settings.ConvertToDatabase();

                    foreach (FavShowData favShowData in settings.TvShows)
                    {
                        db.FavShowData.Add(favShowData);
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Fehler beim Schreiben zu Datenbank", ex);
            }
        }

        public static bool LoadFromDatabase(Settings settings)
        {
            bool result = false;
            try
            {
                string path = GetDBPath();

                if (File.Exists(path))
                {
                    using (CustomDbContext db = new CustomDbContext())
                    {
                        System.Data.Entity.Database.SetInitializer<CustomDbContext>(null);

                        db.Database.Initialize(false);

                        db.FavShowData.Load();
                        db.FavSeasonData.Load();
                        db.FavEpisodeData.Load();
                        //db.ShowInformation.Load(); // Not currently used - Calvin 13-Feb-2016
                        //db.SeasonInformation.Load(); // Not currently used - Calvin 13-Feb-2016
                        db.EpisodeInformation.Load();
                        db.DownloadData.Load();
                        db.UploadData.Load();
                        db.SeasonData.Load();
                        db.ShowData.Load();

                        settings.TvShows.Clear();
                        foreach (FavShowData show in db.FavShowData)
                        {
                            settings.TvShows.Add(show);
                        }
                        settings.ConvertFromDatabase();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Fehler beim Lesen von Datenbank", ex);
            }

            return result;
        }
    }
}
