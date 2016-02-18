using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Serialization;

namespace SjUpdater.Model
{
    public class SeasonData : Database.IDatabaseCompatibility
    {
        public SeasonData()
        {
            InDatabase = false;

            Title = "";
            Description = "";
            Url = "";
            CoverUrl = "";
            Show = null;
        }

        [Key]
        public int Id { get; set; }

        [NotMapped]
        [XmlIgnore]
        public bool InDatabase { get; set; }

        public String Title { get; set; }
        public String Description { get; set; }
        public String Url { get; set; }
        public String CoverUrl { get; set; }

        public int ShowId { get; set; }
        [ForeignKey("ShowId")]
        public ShowData Show { get; set; }

        public void AddToDatabase(Database.CustomDbContext db)
        {
            if (db == null)
                return;

            if (!InDatabase)
            {
                InDatabase = true;

                if (Show != null)
                    Show.AddToDatabase(db);

                Database.DatabaseWriter.AddToDatabase<SeasonData>(db.SeasonData, this);
            }
        }

        public void RemoveFromDatabase(Database.CustomDbContext db)
        {
            if (db == null)
                return;

            if (InDatabase)
            {
                InDatabase = false;

                Database.DatabaseWriter.RemoveFromDatabase<SeasonData>(db.SeasonData, this);

                if (Show != null)
                {
                    Show.RemoveFromDatabase(db);
                    Show = null;
                }
            }
        }
    }
}
