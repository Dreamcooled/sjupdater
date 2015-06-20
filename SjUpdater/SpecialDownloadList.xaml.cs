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
    /// Interaction logic for SpecialDownloadList.xaml
    /// </summary>
    public partial class SpecialDownloadList : UserControl
    {
        public SpecialDownloadList()
        {
            InitializeComponent();
        }
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource",
          typeof(IEnumerable), typeof(SpecialDownloadList), new FrameworkPropertyMetadata(null));

        public IEnumerable ItemsSource
        {
            get
            {
                return (IEnumerable)GetValue(SpecialDownloadList.ItemsSourceProperty);
            }
            set
            {
                this.SetValue(SpecialDownloadList.ItemsSourceProperty, value);
            }
        }

        public static readonly DependencyProperty DownloadCommandProperty = DependencyProperty.Register("DownloadCommand",
     typeof(ICommand), typeof(SpecialDownloadList), new FrameworkPropertyMetadata(null));

        public ICommand DownloadCommand
        {
            get
            {
                return (ICommand)GetValue(SpecialDownloadList.DownloadCommandProperty);
            }
            set
            {
                this.SetValue(SpecialDownloadList.DownloadCommandProperty, value);
            }
        }
    }
}
