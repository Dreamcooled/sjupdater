using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using SjUpdater.Annotations;

namespace SjUpdater
{
    public class EpisodeView : INotifyPropertyChanged
    {

        public EpisodeView()
        {

            EpisodeDescriptors = new List<EpisodeDescriptor>();


        }

        public int Episode
        {
            get
            {
                if (EpisodeDescriptors.Any())
                {
                    return EpisodeDescriptors.First().Episode;
                }
                return -1;
            }
        }

        public List<EpisodeDescriptor> EpisodeDescriptors
        {
            get;
            private set;
        }

        public string DetailTitle
        {
            get
            {
                String s = EpisodeDescriptors.First().SeasonShowName + " ";
                if (EpisodeDescriptors.First().Season != -1)
                {
                    s += "Season " + EpisodeDescriptors.First().Season +" ";
                    if (Episode != -1)
                    {
                        s += "Episode " + Episode;
                    }
                }
                return s;
            }
        }
        public string Title
        {
            get
            {
                if (EpisodeDescriptors.First().Season == -1)
                {
                    return EpisodeDescriptors.First().Title;
                }
                if (Episode == -1)
                {
                    return EpisodeDescriptors.Count().ToString() + " Others";
                } 
                return "Episode" + Episode;

            }
        }

        public string Languages
        {
            get
            {
                EpisodeDescriptor.Lang langs = EpisodeDescriptors.Aggregate<EpisodeDescriptor, EpisodeDescriptor.Lang>(0, (current, episodeDescriptor) => current | episodeDescriptor.Language);

                switch (langs)
                {
                    case EpisodeDescriptor.Lang.English:
                        return "English";
                    case EpisodeDescriptor.Lang.German:
                        return "German";
                    case EpisodeDescriptor.Lang.Both:
                        return "German,English";
                }
                return "";

            }
        }

        public string Formats
        {

            get
            {
               var lisFormats = new List<string>();
                var lisFormatsComp = new List<string>();
                foreach (var descriptor in EpisodeDescriptors)
                {
                    string f = descriptor.Format;
                    if(String.IsNullOrWhiteSpace(f))
                        continue;
                    if(!lisFormatsComp.Contains(f.ToLower())){
                        lisFormats.Add(f);
                        lisFormatsComp.Add(f.ToLower());
                   }
                }

                return string.Join(",", lisFormats);

            }
        }


        public Brush Background
        {
            get
            {

                var colors = new Color[]
                {
                    Color.FromRgb(111, 189, 69),
                    Color.FromRgb(75, 179, 221),
                    Color.FromRgb(65, 100, 165),
                    Color.FromRgb(225, 32, 38),
                    Color.FromRgb(128, 0, 128),
                    Color.FromRgb(0, 128, 64),
                   Color.FromRgb(0, 148, 255),
                    Color.FromRgb(255, 0, 199),
                    Color.FromRgb(255, 135, 15),
                   Color.FromRgb(45, 255, 87),
                    Color.FromRgb(127, 0, 55)
                };

                int season = EpisodeDescriptors.First().Season;
                if (season == -1) season = 0;
                Color c = colors[season%colors.Length];
                Color cb = Colors.Black;
                float a = (Episode == -1) ? 0.8f : 0.5f;
                byte r = (byte) (a*cb.R + (1 - a)*c.R);
                byte g = (byte)(a * cb.G + (1 - a) * c.G);
                byte b = (byte)(a * cb.B + (1 - a) * c.B);
                return new SolidColorBrush(Color.FromRgb(r,g,b));


            }
        }

        #region tile stuff

        public bool IsDoubleWidth { 
            get 
            { 
                 return Episode == -1;
            } 
        }


        public bool IsPressed
        {
            get { return _isPressed; }
            set
            {
                if (_isPressed != value)
                {
                    _isPressed = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool _isPressed;
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        } 
        #endregion
    }
}
