using System;
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

            }
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
