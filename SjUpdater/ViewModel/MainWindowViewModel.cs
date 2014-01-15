using System.Collections.Generic;
using System.Collections.ObjectModel;
using SjUpdater.Model;

namespace SjUpdater.ViewModel
{
    public class MainWindowViewModel
    {

        public MainWindowViewModel(ObservableCollection<FavShowData> shows)
        {
            _vm = new OverviewPanormaViewModel(shows);
            _list = new List<OverviewPanormaViewModel> {_vm};
        }

        private readonly OverviewPanormaViewModel _vm;
        private readonly List<OverviewPanormaViewModel> _list;
        public List<OverviewPanormaViewModel> PanoramaItems
        {
            get { return _list; }
        }


       /* public event EventHandler AddNew
        {
            add { _vm.AddNew += value; }
            remove { _vm.AddNew -= value; }
        }
        public event EventHandler<ShowViewModel> OpenShow
        {
            add { _vm.OpenShow += value; }
            remove { _vm.OpenShow -= value; }
        }*/



    }
}
