using System;
using System.Collections;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SjUpdater
{
    /// <summary>
    /// Interaction logic for DownloadPopupList.xaml
    /// </summary>
    public partial class DownloadPopupList : UserControl
    {
        public DownloadPopupList()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource",
            typeof (IEnumerable), typeof (DownloadPopupList), new FrameworkPropertyMetadata(null));

        public IEnumerable ItemsSource
        {
            get
            {
                return (IEnumerable)GetValue(DownloadPopupList.ItemsSourceProperty);
            }
            set
            {
                this.SetValue(DownloadPopupList.ItemsSourceProperty, value);
            }
        }

        public static readonly DependencyProperty DownloadCommandProperty = DependencyProperty.Register("DownloadCommand",
     typeof(ICommand), typeof(DownloadPopupList), new FrameworkPropertyMetadata(null));

        public ICommand DownloadCommand
        {
            get
            {
                return (ICommand)GetValue(DownloadPopupList.DownloadCommandProperty);
            }
            set
            {
                this.SetValue(DownloadPopupList.DownloadCommandProperty, value);
            }
        }


        public static readonly DependencyProperty ShowFavColumnProperty = DependencyProperty.Register("ShowFavColumn",
     typeof(bool), typeof(DownloadPopupList), new FrameworkPropertyMetadata(true));

        public bool ShowFavColumn
        {
            get
            {
                return (bool)GetValue(DownloadPopupList.ShowFavColumnProperty);
            }
            set
            {
                this.SetValue(DownloadPopupList.ShowFavColumnProperty, value);
            }
        }





    }




}
