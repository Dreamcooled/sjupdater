using System;
using System.ComponentModel.DataAnnotations;

namespace SjUpdater.Model
{
    public class ShowData : Database.IDatabaseCompatibility

    {
        public ShowData()
        {
            Name = "";
            Url = "";
        }

        [Key]
        public int Id { get; set; }

        public String Name { get; set; }
        public String Url { get; set; }

        public void ConvertToDatabase()
        {
        }

        public void ConvertFromDatabase()
        {
        }
    }
}
