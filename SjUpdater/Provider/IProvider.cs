using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SjUpdater.Provider
{

    public class ShowInformation : Database.IDatabaseCompatibility
    {
        public ShowInformation()
        {
            InDatabase = false;
        }

        [Key]
        public int Id { get; set; }

        [NotMapped]
        [XmlIgnore]
        public bool InDatabase { get; set; }

        public String Title { get; set; }
        public String Status { get; set; }
        public int? NumberEpisodes { get; set; }
        public int? NumberSeasons { get; set; }
        public String ProviderHomepage { get; set; }
        public String PublisherHomepage { get; set; }

        //Only available if withNextPrevEp = true
        public DateTime? PreviousEpisodeDate { get; set; }
        public DateTime? NextEpisodeDate { get; set; }
        public int? PreviousEpisodeSeasonNr { get; set; }
        public int? PreviousEpisodeEpisodeNr { get; set; }
        public int? NextEpisodeSeasonNr { get; set; }
        public int? NextEpisodeEpisodeNr { get; set; }

        //Only available if withImages=true
        public object Backdrops { get; set; }
        public object Posters { get; set; }
        public String Poster { get; set; }
        public String Backdrop { get; set; }

        public void ConvertToDatabase(bool cascade = true)
        {
        }

        public void ConvertFromDatabase(bool cascade = true)
        {
            InDatabase = true;
        }

        public void AddToDatabase(Database.CustomDbContext db)
        {
            if (db == null)
                return;

            if (!InDatabase)
            {
                InDatabase = true;
                ConvertToDatabase(false);

                //Database.DatabaseWriter.AddToDatabase<ShowInformation>(db.ShowInformation, this); // not used at the moment
            }
        }

        public void RemoveFromDatabase(Database.CustomDbContext db)
        {
            if (db == null)
                return;

            if (InDatabase)
            {
                InDatabase = false;

                //Database.DatabaseWriter.RemoveFromDatabase<ShowInformation>(db.ShowInformation, this); // not used at the moment
            }
        }
    }

    public class SeasonInformation : Database.IDatabaseCompatibility
    {
        public SeasonInformation()
        {
            InDatabase = false;
        }

        [Key]
        public int Id { get; set; }

        [NotMapped]
        [XmlIgnore]
        public bool InDatabase { get; set; }

        public String Title { get; set; }
        public String Overview { get; set; }
        public DateTime? AirDate { get; set; }
        public int? NumberEpisodes { get; set; }
        public String ProviderHomepage { get; set; }
        public String PublisherHomepage { get; set; }

        //Only available if withImages=true
        public object Backdrops { get; set; }
        public object Posters { get; set; }
        public String Poster { get; set; }
        public String Backdrop { get; set; }

        public void ConvertToDatabase(bool cascade = true)
        {
        }

        public void ConvertFromDatabase(bool cascade = true)
        {
            InDatabase = true;
        }

        public void AddToDatabase(Database.CustomDbContext db)
        {
            if (db == null)
                return;

            if (!InDatabase)
            {
                InDatabase = true;
                ConvertToDatabase(false);

                //Database.DatabaseWriter.AddToDatabase<SeasonInformation>(db.SeasonInformation, this); // not used at the moment
            }
        }

        public void RemoveFromDatabase(Database.CustomDbContext db)
        {
            if (db == null)
                return;

            if (InDatabase)
            {
                InDatabase = false;

                //Database.DatabaseWriter.RemoveFromDatabase<SeasonInformation>(db.SeasonInformation, this); // not used at the moment
            }
        }
    }
    public class EpisodeInformation : Database.IDatabaseCompatibility
    {
        public EpisodeInformation()
        {
            InDatabase = false;
        }

        [Key]
        public int Id { get; set; }

        [NotMapped]
        [XmlIgnore]
        public bool InDatabase { get; set; }

        public String Title { get; set; }
        [XmlIgnore] //To save storage TODO: remove
        public String Overview { get; set; }
        public DateTime? AirDate{ get; set; }
        public String ProviderHomepage { get; set; }
        public String PublisherHomepage { get; set; }

        //Only available if withImages=true
        [XmlIgnore] //To save storage. TODO: remove
        public object Images { get; set; }
        public String Image { get; set; }

        public void ConvertToDatabase(bool cascade = true)
        {
        }

        public void ConvertFromDatabase(bool cascade = true)
        {
            InDatabase = true;
        }

        public void AddToDatabase(Database.CustomDbContext db)
        {
            if (db == null)
                return;

            if (!InDatabase)
            {
                InDatabase = true;

                Database.DatabaseWriter.AddToDatabase<EpisodeInformation>(db.EpisodeInformation, this);
            }
        }

        public void RemoveFromDatabase(Database.CustomDbContext db)
        {
            if (db == null)
                return;

            if (InDatabase)
            {
                InDatabase = false;
                ConvertToDatabase(false);

                Database.DatabaseWriter.RemoveFromDatabase<EpisodeInformation>(db.EpisodeInformation, this);
            }
        }
    }


    public interface IProvider
    {
        object FindShow(String name);
        ShowInformation GetShowInformation(object show,bool withImages= true, bool withPreviousNextEp=true);

        SeasonInformation GetSeasonInformation(object show, int season, bool withImages = true);

        EpisodeInformation GetEpisodeInformation(object show, int season, int episode, bool withImages = true);

        String GetFirstImage(object images);
        String GetImage(int? maxwidth = null, int? maxheight = null);
    }

 


}
