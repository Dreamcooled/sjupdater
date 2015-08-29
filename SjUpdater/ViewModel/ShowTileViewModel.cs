using System;
using System.Windows;
using System.Windows.Threading;
using SjUpdater.Model;
using SjUpdater.Utils;

namespace SjUpdater.ViewModel
{
    public class ShowTileViewModel : PropertyChangedImpl
    {
        private readonly FavShowData _show;
        private CachedBitmap _bitmap;
        private String _title;
        private String _numberText;
        private String _nextText;
        private String _prevText;
        private Visibility _newEpisodesVisible = Visibility.Collapsed;
        private Visibility _isLoadingVisible = Visibility.Collapsed;
        private ShowViewModel _showViewModel;
        private readonly Dispatcher _dispatcher;

        public ShowTileViewModel(FavShowData show)
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _show = show;
           // _showViewModel  = new ShowViewModel(_show);
        

            Title= _show.Name;
            IsLoadingVisible = (_show.IsLoading) ? Visibility.Visible : Visibility.Collapsed;
            RecalcText();
            RecalcNextPrevEpText();
            if (!String.IsNullOrWhiteSpace(_show.Cover))
                Background = new CachedBitmap(_show.Cover);
            else
                Background = null;
            _show.PropertyChanged += _show_PropertyChanged;

        }

        void _show_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FavShowData.Name))
            { 
                Title = _show.Name;
            } 
            else if (e.PropertyName == nameof(FavShowData.Cover))
            {
                _dispatcher.Invoke(delegate
                {
                    Background = new CachedBitmap(_show.Cover);
                });

            } else if (e.PropertyName == nameof(FavShowData.NumberOfEpisodes) || e.PropertyName == nameof(FavShowData.NumberOfSeasons))
            {
                RecalcText();
            } else if (e.PropertyName == nameof(FavShowData.NewEpisodes))
            {
                NewEpisodesVisible = (_show.NewEpisodes) ? Visibility.Visible : Visibility.Collapsed;
            } else if (e.PropertyName == nameof(FavShowData.IsLoading))
            {
                IsLoadingVisible = (_show.IsLoading) ? Visibility.Visible : Visibility.Collapsed;
            } else if (e.PropertyName == nameof(FavShowData.Status) || e.PropertyName.StartsWith("NextEpisode") ||
                       e.PropertyName.StartsWith("PreviousEpisode"))
            {
                RecalcNextPrevEpText();
            }
        }

        private void RecalcNextPrevEpText()
        {
            String next="";
            String prev = "";
            if (_show.Status == null) return;
            if (_show.Status == "Ended" || _show.Status == "Cancelled")
            {
                next = "Show Ended";
                prev = "Final Episode aired ";
            }
            if (_show.NextEpisodeDate.HasValue)
            {
                if (_show.NextEpisodeSeasonNr == 1 && _show.NextEpisodeEpisodeNr == 1)
                {
                    next = "Pilot airs ";
                }
                else if (_show.NextEpisodeEpisodeNr == 1)
                {
                    next = "S" + _show.NextEpisodeSeasonNr + " airs ";
                }
                else
                {
                    next = "S" + _show.NextEpisodeSeasonNr + "E" + _show.NextEpisodeEpisodeNr + " airs ";
                }
                TimeSpan ts = _show.NextEpisodeDate.Value - DateTime.Today;
                if (ts.Days == 1)
                {
                    next += "tomorrow";
                }
                else if (ts.Days < 30)
                {
                    next += "in " + ts.Days + " days";
                }
                else if (ts.Days < 360)
                {
                    next += "in " + ts.Days/30 + " months";
                }
                else
                {
                    next += "in "+ ts.Days/360+" years";
                }

            }
            else if(_show.Status == "Returning Series")
            {
                next = "Next air date unknown";
            }

            if (_show.PreviousEpisodeDate.HasValue)
            {
                if (_show.Status == "Returning Series")
                {
                    prev = "S" + _show.PreviousEpisodeSeasonNr + "E" + _show.PreviousEpisodeEpisodeNr + " aired ";
                }
                TimeSpan ts = DateTime.Today -_show.PreviousEpisodeDate.Value;
                if (ts.Days == 0)
                {
                    prev = "today";
                } else if (ts.Days == 1)
                {
                    prev += "yesterday";
                }
                else if (ts.Days < 30)
                {
                    prev += ts.Days + " days ago";
                }
                else if (ts.Days < 360)
                {
                    prev += ts.Days / 30 + " months ago";
                }
                else
                {
                    prev += ts.Days / 360 + " years ago";
                }
            }
            
            NextText = next;
            PrevText = prev;
        }

        private void RecalcText()
        {
            String n = _show.NumberOfEpisodes + " ";
            n += _show.NumberOfEpisodes > 1 ? "Episodes" : "Episode";
            n += " in " + _show.NumberOfSeasons + " ";
            n += _show.NumberOfSeasons > 1 ? "Seasons" : "Season";
            NumberText = n;
        }


        public FavShowData Show
        {
            get { return _show; }
        }

        public ShowViewModel ShowViewModel
        {
            get { return _showViewModel ?? (_showViewModel = new ShowViewModel(_show)); }
        }

        public string NumberText
        {
            get { return _numberText; }

            private set
            {
                _numberText = value;
                OnPropertyChanged();
            }
        }

        public String NextText
        {
            get { return _nextText;}
            private set
            {
                _nextText = value;
                OnPropertyChanged();
            }
        }

        public String PrevText
        {
            get { return _prevText; }
            private set
            {
                _prevText = value;
                OnPropertyChanged();
            }
        }

        public Visibility NewEpisodesVisible
        {
            get { return _newEpisodesVisible; }

            private set
            {
                _newEpisodesVisible = value;
                OnPropertyChanged();
            }
        }

        public Visibility IsLoadingVisible
        {
            get { return _isLoadingVisible; }

            private set
            {
                _isLoadingVisible = value;
                OnPropertyChanged();
            }
        }

        public string Title
        {
            get { return _title; }

            private set
            {
                _title = value; 
                OnPropertyChanged();
            }
        }


        public CachedBitmap Background
        {
            get { return _bitmap; }

            private set
            {
                _bitmap = value;
                OnPropertyChanged();
            }
        }

        public Visibility BackgroundImageVisibility
        { get { return Settings.Instance.EnableImages ? Visibility.Visible : Visibility.Collapsed; } }

        public Visibility BackgroundRectangleVisibility
        { get { return Settings.Instance.EnableImages ? Visibility.Collapsed : Visibility.Visible; } }
    }
}
