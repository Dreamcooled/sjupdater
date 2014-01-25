using System;
using System.Windows;
using System.Windows.Threading;
using SjUpdater.Model;
using SjUpdater.Utils;

namespace SjUpdater.ViewModel
{
    public class ShowTileViewModel : PropertyChangedImpl
    {
       // private readonly SimpleCommand<object, ShowViewModel> _clickedCommandDest;
      //  private readonly ICommand _clickedCommand;



        private readonly FavShowData _show;
        private  CachedBitmap _bitmap;
        private String _title;
        private String _numberText;
        private Visibility _newEpisodesVisible = Visibility.Collapsed;
        private  Visibility _isLoadingVisible = Visibility.Collapsed;
        private readonly ShowViewModel _showViewModel;
        private readonly Dispatcher _dispatcher;

        public ShowTileViewModel(FavShowData show)//,  SimpleCommand<object, ShowViewModel> clickedCommand )
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _show = show;
           // _clickedCommandDest = clickedCommand;
            _showViewModel  = new ShowViewModel(_show);
         /*   _clickedCommand = new SimpleCommand<object, object>(o =>
            {
                if (_showViewModel == null)
                {
                    _showViewModel = new ShowViewModel(_show);
                }
                _clickedCommandDest.Execute(_showViewModel);
            });*/

            Title= _show.Name;
            IsLoadingVisible = (_show.IsLoading) ? Visibility.Visible : Visibility.Collapsed;
            RecalcText();
            if (!String.IsNullOrWhiteSpace(_show.Cover))
                Background = new CachedBitmap(_show.Cover);
            else
                Background = null;
            _show.PropertyChanged += _show_PropertyChanged;

        }

        void _show_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Name")
            { 
                Title = _show.Name;
            } 
            else if (e.PropertyName == "Cover")
            {
                _dispatcher.Invoke(delegate
                {
                    Background = new CachedBitmap(_show.Cover);
                });

            } else if (e.PropertyName == "NumberOfEpisodes" || e.PropertyName == "NumberOfShows")
            {
                RecalcText();
            } else if (e.PropertyName == "NewEpisodes")
            {
                NewEpisodesVisible = (_show.NewEpisodes) ? Visibility.Visible : Visibility.Collapsed;
            } else if (e.PropertyName == "IsLoading")
            {
                IsLoadingVisible = (_show.IsLoading) ? Visibility.Visible : Visibility.Collapsed;
            }
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
            get { return _showViewModel; }
        }
        /* public ICommand ClickedCommand
        {
            get { return _clickedCommand; }
        }*/


        public string NumberText
        {
            get { return _numberText; }

            private set
            {
                _numberText = value;
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
    }
}
