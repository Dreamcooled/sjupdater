using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using SjUpdater.Utils;

namespace SjUpdater.ViewModel
{
    public class MultiSelectionViewModel : PropertyChangedImpl
    {


        public MultiSelectionViewModel()
        {
            MarkSelectedAsDownloadedCommand = new SimpleCommand<object,object>(delegate
            {
                foreach (var episodeViewModel in SelectedEpisodes)
                {
                    episodeViewModel.Episode.Downloaded = true;
                }
                OnPropertyChanged("InfoText2");
            });
            MarkSelectedAsWatchedCommand = new SimpleCommand<object, object>(delegate
            {
                foreach (var episodeViewModel in SelectedEpisodes)
                {
                    episodeViewModel.Episode.Watched = true;
                    episodeViewModel.Episode.Downloaded = true;
                }
                OnPropertyChanged("InfoText2");
            });
            UnmarkSelectedCommand= new SimpleCommand<object, object>(delegate
            {
                foreach (var episodeViewModel in SelectedEpisodes)
                {
                    episodeViewModel.Episode.Watched = false;
                    episodeViewModel.Episode.Downloaded = false;
                }
                OnPropertyChanged("InfoText2");
            });
        }

        private List<EpisodeViewModel> _selectedEpisodes = null; 
        public List<EpisodeViewModel> SelectedEpisodes
        {
            get { return _selectedEpisodes;}
            set
            {
                _selectedEpisodes = value;
                OnPropertyChanged("InfoText");
                OnPropertyChanged("InfoText2");
            }
        }

        public String InfoText
        {
            get
            {
                int nrEpisodes = SelectedEpisodes.Count;

                String info = nrEpisodes + " Episodes";
                int seasons = SelectedEpisodes.Select(ev => ev.Episode.Season).Distinct().Count();
                if (seasons == 1)
                {
                    info += " in 1 Season";
                }
                else
                {
                    info += " in " + seasons + " Seasons";
                }
                return info;
            }
        }

        public String InfoText2
        {
            get
            {
                int nrDownloaded = SelectedEpisodes.Count(ev => ev.Episode.Downloaded);
                int nrWatched = SelectedEpisodes.Count(ev => ev.Episode.Watched);

                return nrDownloaded + " Downloaded, " + nrWatched + " Wachted";
            }
        }

        public ICommand MarkSelectedAsWatchedCommand { get; private set; }
        public ICommand MarkSelectedAsDownloadedCommand { get; private set; }
        public ICommand UnmarkSelectedCommand { get; private set; }


    }



}
