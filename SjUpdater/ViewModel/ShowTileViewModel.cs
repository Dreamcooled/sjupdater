using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using SjUpdater.Annotations;
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
        private String _bottomText;
        private Visibility _bottomVisible = Visibility.Collapsed;
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
            Background = !String.IsNullOrWhiteSpace(_show.Cover) ? new CachedBitmap(_show.Cover) : null;
            _show.PropertyChanged += _show_PropertyChanged;

        }

        void _show_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(FavShowData.Name):
                    Title = _show.Name;
                    break;
                case nameof(FavShowData.Cover):
                    _dispatcher.Invoke(delegate
                    {
                        Background = new CachedBitmap(_show.Cover);
                    });
                    break;
                case nameof(FavShowData.NumberOfEpisodes):
                case nameof(FavShowData.NumberOfSeasons):
                    RecalcText();
                    break;
                case nameof(FavShowData.IsLoading):
                    IsLoadingVisible = (_show.IsLoading) ? Visibility.Visible : Visibility.Collapsed;
                    break;
                default:
                    if (e.PropertyName == nameof(FavShowData.Status) || e.PropertyName.StartsWith("NextEpisode") ||
                        e.PropertyName.StartsWith("PreviousEpisode") || e.PropertyName == nameof(FavShowData.NewEpisodes) || e.PropertyName == nameof(FavShowData.NewUpdates))
                    {
                        RecalcNextPrevEpText();
                    }
                    break;
            }
        }

        public void RecalcNextPrevEpText()
        {
            String next="";
            String prev = "";
            Visibility bottomVis = Visibility.Collapsed;
            String bottomText ="";
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
            
                bottomText = next;
                bottomVis = Visibility.Visible;
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

            const int numEpUpdates = 4;

            if (_show.NewEpisodes)
            {
                var eps = _show.Seasons.SelectMany(s => s.Episodes.Where(e => e.NewEpisode)).ToList();
                if (eps.Any())
                {
                    bottomText = "New:";
                    bottomVis = Visibility.Visible;
                    for (int i = 0; i < Math.Min(numEpUpdates, eps.Count); i++)
                    {
                        bottomText += " S" + eps[i].Season.Number + "E" + eps[i].Number;
                    }
                    if (eps.Count > numEpUpdates) bottomText += "...";

                    if (Show.NewUpdates)
                    {
                        bottomText += " +Updates";
                    }
                }
            } else if (_show.NewUpdates)
            {
                var eps = _show.Seasons.SelectMany(s => s.Episodes.Where(e => e.NewUpdate)).ToList();
                if (eps.Any())
                {
                    bottomText = "Updated:";
                    bottomVis = Visibility.Visible;
                    for (int i = 0; i < Math.Min(numEpUpdates, eps.Count); i++)
                    {
                        bottomText += " S" + eps[i].Season.Number + "E" + eps[i].Number;
                    }
                    if (eps.Count > numEpUpdates) bottomText += "...";
                }
            }

            NextText = next;
            PrevText = prev;
            BottomText = bottomText;
            BottomVisible = bottomVis;
        }

        private void RecalcText()
        {
            String n = _show.NumberOfEpisodes + " ";
            n += _show.NumberOfEpisodes > 1 ? "Episodes" : "Episode";
            n += " in " + _show.NumberOfSeasons + " ";
            n += _show.NumberOfSeasons > 1 ? "Seasons" : "Season";
            NumberText = n;
        }


        public FavShowData Show => _show;

        public ShowViewModel ShowViewModel => _showViewModel ?? (_showViewModel = new ShowViewModel(_show));

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
        public String BottomText
        {
            get { return _bottomText;}
            set
            {
                _bottomText = value;
                OnPropertyChanged();
            }
        }

        public Visibility BottomVisible
        {
            get { return _bottomVisible;}
            set
            {
                _bottomVisible = value;
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
    }
}
