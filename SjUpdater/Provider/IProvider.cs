using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SjUpdater.Provider
{

    public class ShowInformation
    {
        public String Title { get; set; }
        public  bool? IsActive { get; set; }
        public int? NumberEpisodes { get; set; }
        public int? NumberSeasons { get; set; }
        public String ProviderHomepage { get; set; }
        public String PublisherHomepage { get; set; }
        public object Backdrops { get; set; }
        public object Posters { get; set; }
        public String Poster { get; set; }
        public String Backdrop { get; set; }
    }

    public class SeasonInformation
    {
        public String Title { get; set; }
        public String Overview { get; set; }
        public DateTime? AirDate { get; set; }
        public int? NumberEpisodes { get; set; }
        public String ProviderHomepage { get; set; }
        public String PublisherHomepage { get; set; }
        public object Backdrops { get; set; }
        public object Posters { get; set; }
        public String Poster { get; set; }
        public String Backdrop { get; set; }
    }
    public class EpisodeInformation
    {
        public String Title{ get; set; }
        [XmlIgnore] //To save storage TODO: remove
        public String Overview { get; set; }
        public DateTime? AirDate{ get; set; }
        public String ProviderHomepage { get; set; }
        public String PublisherHomepage { get; set; }
        [XmlIgnore] //To save storage. TODO: remove
        public object Images { get; set; }
        public String Image { get; set; }

    }


    public interface IProvider
    {
        object FindShow(String name);
        ShowInformation GetShowInformation(object show);

        SeasonInformation GetSeasonInformation(object show, int season);

        EpisodeInformation GetEpisodeInformation(object show, int season, int episode);

        String GetFirstImage(object images);
        String GetImage(int? maxwidth = null, int? maxheight = null);
    }

 


}
