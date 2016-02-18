using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Serialization;

namespace SjUpdater.Model
{
    public class DownloadData : Database.IDatabaseCompatibility
    {
        public DownloadData()
        {
            InDatabase = false;

            Title = "";
            Upload = null;
            Links = new Dictionary<string, string>();
        }

        [Key]
        public int Id { get; set; }

        [NotMapped]
        [XmlIgnore]
        public bool InDatabase { get; set; }

        public String Title { get; set; }

        public Dictionary<String,String> Links { get; internal set; }

        public int UploadId { get; set; }
        [ForeignKey("UploadId")]
        public UploadData Upload { get; set; }

        // Used by DatabaseWriter because SQLCE doesn't seem to recognise Dictionary - Calvin 12-Feb-2016
        [XmlIgnore]
        public string LinkString {
            get
            {
                string result = "";

                if (Links != null)
                {
                    string[] LinkKeys = new string[Links.Keys.Count];
                    string[] LinkValues = new string[Links.Values.Count];

                    Links.Keys.CopyTo(LinkKeys, 0);
                    Links.Values.CopyTo(LinkValues, 0);

                    for (int i = 0; i < Links.Keys.Count; i++)
                    {
                        result += LinkKeys[i] + "\t" + LinkValues[i] + "\n";
                    }
                }

                return result;
            }
            set
            {
                Links.Clear();

                foreach (string keyValue in value.Split('\n'))
                {
                    if (keyValue.Length > 0)
                    {
                        string[] keyValueSplit = keyValue.Split('\t');
                        Links.Add(keyValueSplit[0], keyValueSplit[1]);
                    }
                }
            }
        }
        
        public void AddToDatabase(Database.CustomDbContext db)
        {
            if (db == null)
                return;

            if (!InDatabase)
            {
                InDatabase = true;

                if (Upload != null)
                    Upload.AddToDatabase(db); // Causes "adding a relationship with an entity which is in the deleted state is not allowed" errors - Calvin 13-Feb-2016

                Database.DatabaseWriter.AddToDatabase<DownloadData>(db.DownloadData, this);
            }
        }

        public void RemoveFromDatabase(Database.CustomDbContext db)
        {
            if (db == null)
                return;

            if (InDatabase)
            {
                InDatabase = false;

                if (Upload != null)
                {
                    Upload.RemoveFromDatabase(db); // Causes "adding a relationship with an entity which is in the deleted state is not allowed" errors - Calvin 13-Feb-2016
                    Upload = null;
                }

                Database.DatabaseWriter.RemoveFromDatabase<DownloadData>(db.DownloadData, this);

            }
        }
    }
}
