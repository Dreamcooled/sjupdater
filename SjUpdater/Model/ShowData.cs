using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Serialization;

namespace SjUpdater.Model
{
    public class ShowData : Database.IDatabaseCompatibility
    {
        public ShowData()
        {
            InDatabase = false;

            Name = "";
            Url = "";
        }

        [Key]
        public int Id { get; set; }

        [NotMapped]
        [XmlIgnore]
        public bool InDatabase { get; set; }

        public String Name { get; set; }
        public String Url { get; set; }

        public void ConvertToDatabase()
        {
        }

        public void ConvertFromDatabase()
        {
            InDatabase = true;
        }

        public void AddToDatabase(Database.CustomDbContext db)
        {
            if (db == null)
                return;

            if (!InDatabase)
            {
                Database.DatabaseWriter.AddToDatabase<ShowData>(db.ShowData, this);

                InDatabase = true;
            }
        }

        public void RemoveFromDatabase(Database.CustomDbContext db)
        {
            if (db == null)
                return;

            if (InDatabase)
            {
                Database.DatabaseWriter.RemoveFromDatabase<ShowData>(db.ShowData, this);

                InDatabase = false;
            }
        }
    }
}
