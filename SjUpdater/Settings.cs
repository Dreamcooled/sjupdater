using System.Collections.ObjectModel;
using SjUpdater.Model;
using SjUpdater.XML;

namespace SjUpdater
{
    public class Settings
    {
        public ObservableCollection<FavShowData> TvShows{ get; set; }


        public Settings()
        {
            TvShows = new ObservableCollection<FavShowData>();
        }

        public static Settings Load(string filename)
        {

            return XmlSerialization.LoadFromXml<Settings>(filename);
        }

        public void Save(string filename)
        {
            XmlSerialization.SaveToXml(this,filename);
        }
    }
}