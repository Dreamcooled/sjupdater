using System;
using System.ComponentModel.DataAnnotations;

namespace SjUpdater.Model
{
    public class SeasonData : Database.IDatabaseCompatibility
    {
        public SeasonData()
        {
            Title = "";
            Description = "";
            Url = "";
            CoverUrl = "";
            Show = null;
        }

        [Key]
        public int Id { get; set; }

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
            if (Show != null)
                Show.ConvertFromDatabase();
        }
    }
}
