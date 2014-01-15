using System;

namespace SjUpdater.Model
{
    public class SeasonData
    {
        public SeasonData()
        {
            Title = "";
            Description = "";
            Url = "";
            CoverUrl = "";
            Show = null;
        }
        public String Title { get; set; }
        public String Description { get; set; }
        public String Url { get; set; }
        public String CoverUrl { get; set; }
        public ShowData Show { get; set; }
    }
}
