using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Hardcodet.Wpf.TaskbarNotification;
using MahApps.Metro.Controls;
using SjUpdater.Model;
using SjUpdater.Utils;
using SjUpdater.ViewModel;

namespace SjUpdater
{
    /// <summary>
    /// Interaktionslogik für NotificationBalloon.xaml
    /// </summary>
    public partial class NotificationBalloon :UserControl
    {


        public ICommand ShowClickedCommand { get; private set; }
        public NotificationBalloon(IEnumerable<FavShowData> list)
        {
            InitializeComponent();
            ShowClickedCommand = new SimpleCommand<object, ShowViewModel>(OnShowViewClicked);
            ItemsControl.ItemsSource = list.Select(s => new ShowTileViewModel(s));
        }

        public event ShowViewClickedDelegate ShowViewClicked;

        private void OnShowViewClicked(ShowViewModel obj)
        {
            if (ShowViewClicked != null)
            {
                ShowViewClicked(this, obj);
            }
            TaskbarIcon taskbarIcon = TaskbarIcon.GetParentTaskbarIcon(this);
            taskbarIcon.CloseBalloon();
        }

        private void CloseBalloon(object sender, RoutedEventArgs e)
        {
            TaskbarIcon taskbarIcon = TaskbarIcon.GetParentTaskbarIcon(this);
            taskbarIcon.CloseBalloon();
        }

        private void Grid_MouseEnter_1(object sender, MouseEventArgs e)
        {
            TaskbarIcon taskbarIcon = TaskbarIcon.GetParentTaskbarIcon(this);
            taskbarIcon.ResetBalloonCloseTimer();
        }
    }

    public delegate void ShowViewClickedDelegate(object sender, ShowViewModel arg);
}
