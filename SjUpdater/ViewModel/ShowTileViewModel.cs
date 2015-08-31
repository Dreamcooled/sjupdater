using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
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
        private String _newsText;
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
                    if (String.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(FavShowData.Status) || e.PropertyName.StartsWith("NextEpisode") ||
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
            String newsText = "";
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
                next += FormatDate(_show.NextEpisodeDate.Value);
            
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
                prev += FormatDate(_show.PreviousEpisodeDate.Value);
            }

            if (_show.NewEpisodes)
            {
                var eps = _show.Seasons.SelectMany(s => s.Episodes.Where(e => e.NewEpisode)).ToList();
                if (eps.Any())
                {
                    bottomText = "New:" + FormatEpisodes(eps, Show.NewUpdates?4:10);
                    bottomVis = Visibility.Visible;
                    newsText = "New:" + FormatEpisodes(eps, 10);
                    if (Show.NewUpdates)
                    {
                        bottomText += " +Updates";
                        newsText += "\r\nUpdated:";
                        newsText += FormatEpisodes(_show.Seasons.SelectMany(s => s.Episodes.Where(e => e.NewUpdate)).ToList(), 10);
                    }
                }
            } else if (_show.NewUpdates)
            {
                var eps = _show.Seasons.SelectMany(s => s.Episodes.Where(e => e.NewUpdate)).ToList();
                if (eps.Any())
                {
                    bottomText = "Updated:" + FormatEpisodes(eps, 10);
                    newsText += "Updated:" + FormatEpisodes(eps, 10);
                    bottomVis = Visibility.Visible;
                }
            }

            NextText = next;
            PrevText = prev;
            NewsText = newsText;
            BottomText = bottomText;
            BottomVisible = bottomVis;
        }

        /// <summary>
        /// Formats a date relativ to today
        /// Example output: today, tomorrow, yesterday, 5 days ago, in 3 days, in 3 months, 1 year ago, in 10 years
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private string FormatDate(DateTime t)
        {
            string text = "";
            int days = (DateTime.Today - t.Date).Days;
            bool future = t.Date > DateTime.Today;
            if (days==0) return "today";
            if (future)
            {
                text = "in ";
                days = (t.Date - DateTime.Today).Days;
            }
            if (days == 1) return future ? "tomorrow" : "yesterday";
            if (days < 30)
            {
                text += days + " days";
            } else if (days < 360)
            {
                int months = days/30;
                text += months + " month";
                if (months > 1) text += "s";
            }
            else
            {
                int years = days/360;
                text += years + " year";
                if (years > 1) text += "s";
            }

            if (!future) text += " ago";
            return text;
        }

        /// <summary>
        /// Formats the shortnames of a bunch of episodes
        /// Example output: "S1E2 S3E4 ..."
        /// </summary>
        /// <param name="eps"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        private string FormatEpisodes(List<FavEpisodeData> eps, int max)
        {
            string text = "";
            for (int i = 0; i < Math.Min(max, eps.Count); i++)
            {
                text += " S" + eps[i].Season.Number + "E" + eps[i].Number;
            }
            if (eps.Count > max) text += "...";
            return text;
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

        public String NewsText
        {
            get { return _newsText;}
            private set
            {
                _newsText = value;
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
