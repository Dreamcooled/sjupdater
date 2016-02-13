﻿using System;
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
        public ShowData Show { get; set; }

        public void ConvertToDatabase()
        {
            if (Show != null)
                Show.ConvertToDatabase();
        }

        public void ConvertFromDatabase()
        {
            InDatabase = true;

            if (Show != null)
                Show.ConvertFromDatabase();
        }

        public void AddToDatabase(Database.CustomDbContext db)
        {
            if (db == null)
                return;

            if (!InDatabase)
            {
                Database.DatabaseWriter.AddToDatabase<SeasonData>(db.SeasonData, this);

                InDatabase = true;
            }

            if (Show != null)
                Show.AddToDatabase(db);
        }

        public void RemoveFromDatabase(Database.CustomDbContext db)
        {
            if (db == null)
                return;

            if (InDatabase)
            {
                Database.DatabaseWriter.RemoveFromDatabase<SeasonData>(db.SeasonData, this);

                InDatabase = false;
            }

            if (Show != null)
                Show.RemoveFromDatabase(db);
        }
    }
}
