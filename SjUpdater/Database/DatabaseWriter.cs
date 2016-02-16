using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Data.Entity;
using SjUpdater.Model;

namespace SjUpdater.Database
{
    class DatabaseWriter
    {
        public static CustomDbContext db = null;

        public static string GetDBPath()
        {
            return ConfigurationManager.ConnectionStrings["SjUpdater.Database.CustomDbContext"].ConnectionString.Split('=')[1];
        }

        //static string mutexName = ""; // For debugging

        static Mutex dbMutex = new Mutex();

        public static void Commit()
        {
            dbMutex.WaitOne();

            db.SaveChanges();

            dbMutex.ReleaseMutex();
        }

        public static void AddToDatabase<T>(DbSet<T> set, T entity) where T : class
        {
            dbMutex.WaitOne();

            //mutexName = entity.ToString() + "-add-hold";
            try
            {
                set.Add(entity);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not add '" + entity.ToString() + "' to table '" + set.ToString() + "'", ex);
            }
            finally
            {
                //mutexName = entity.ToString() + "-add-release";

                dbMutex.ReleaseMutex();
            }
        }

        public static void RemoveFromDatabase<T>(DbSet<T> set, T entity) where T : class
        {
            dbMutex.WaitOne();

            //mutexName = entity.ToString() + "-rem-hold";
            try
            {
                set.Remove(entity);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not remove '" + entity.ToString() + "' from table '" + set.ToString() + "'", ex);
            }
            finally
            {
                //mutexName = entity.ToString() + "-rem-release";

                dbMutex.ReleaseMutex();
            }
        }

        public static void SaveToDatabase(Settings settings)
        {
            dbMutex.WaitOne();
            //mutexName = "savetodb";

            try
            {
                string path = GetDBPath();

                if (db == null)
                {
                    if (File.Exists(path))
                        File.Delete(path);

                    db = new CustomDbContext();
                }

                settings.ConvertToDatabase();

                if (File.Exists(path))
                {
                    // If we have an existing db, we should already have some pending changes to save - Calvin 13-Feb-2016

                    db.SaveChanges();
                }
                else
                {
                    // If we do not have an existing db, create a new one and add everything - Calvin 13-Feb-2016
                    db.Database.Create();

                    db.FavShowData.AddRange(settings.TvShows);
                    db.SaveChanges();

                    foreach (FavShowData favShowData in settings.TvShows)
                    {
                        favShowData.InDatabase = true;
                    }
                }
                
            }
            catch (Exception ex)
            {
                db = null;
                throw new Exception("Fehler beim Schreiben zu Datenbank", ex);
            }
            finally
            {
                dbMutex.ReleaseMutex();
            }

        }

        public static bool LoadFromDatabase(Settings settings)
        {
            bool result = false;

            dbMutex.WaitOne();
            //mutexName = "savetodb";

            try
            {
                string path = GetDBPath();

                if (File.Exists(path))
                {
                    db = new CustomDbContext();

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

                    foreach (FavShowData favShowData in db.FavShowData)
                        settings.TvShows.Add(favShowData);

                    settings.ConvertFromDatabase();
                }
            }
            catch (Exception ex)
            {
                db = null;
                throw new Exception("Fehler beim Lesen von Datenbank", ex);
            }

            dbMutex.ReleaseMutex();

            return result;
        }
    }
}