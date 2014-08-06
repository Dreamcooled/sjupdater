using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using Amib.Threading;
using MahApps.Metro;
using MahApps.Metro.Controls;
using SjUpdater.Model;
using SjUpdater.Utils;
using SjUpdater.ViewModel;

namespace SjUpdater
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private Settings setti;
        private readonly MainWindowViewModel _viewModel;
        private bool user_read_updates = true;
        public MainWindow()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;


            AddShowCommand = new SimpleCommand<object, object>(AddShowClicked);
            DownloadCommand = new SimpleCommand<object, String>(DownloadCommandExecute);
            EpisodeClickedCommand = new SimpleCommand<object, EpisodeViewModel>(OnEpisodeViewClicked);
            ShowClickedCommand = new SimpleCommand<object, ShowViewModel>(OnShowViewClicked);
            SettingsCommand = new SimpleCommand<object, object>(SettingsClicked);
            IconClickedCommand = new SimpleCommand<object, object>(IconClicked);
            TerminateCommand = new SimpleCommand<object, object>(Terminate);

            setti = Settings.Instance;
            currentAccent = ThemeManager.DefaultAccents.First(x => x.Name ==  setti.ThemeAccent);


            InitializeComponent();

            CurrentAccent = setti.ThemeAccent;

            Timer t = new Timer(15*60*1000); //15min
            t.Elapsed += t_Elapsed;
            t.Start();
            
            update();

            _viewModel = new MainWindowViewModel(setti.TvShows);
            ShowsPanorama.ItemsSource = _viewModel.PanoramaItems;
            SwitchPage(0);

            if (Environment.GetCommandLineArgs().Contains("-nogui"))
            {
               Hide();
            }
        }

        void t_Elapsed(object sender, ElapsedEventArgs e)
        {
            update();
        }


        private void update()
        {

            SmartThreadPool stp = new SmartThreadPool();
            stp.MaxThreads = (int) setti.NumFetchThreads;
            var results = new List<IWaitableResult>();
            foreach (FavShowData t in setti.TvShows)
            {
                results.Add(stp.QueueWorkItem(data => data.Fetch(), t)); //schedule update of show (executed paralell)
            }
            //wait for completion
            stp.QueueWorkItem(() =>
            {
                SmartThreadPool.WaitAll(results.ToArray());
                var updates = setti.TvShows.Where(show => show.NewEpisodes).ToList();
                if (updates.Count > 0 && user_read_updates)
                {
                    Dispatcher.Invoke(() =>
                    {
                        var n = new NotificationBalloon(updates);
                        n.ShowViewClicked += on_ShowViewClicked;
                        NotifyIcon.ShowCustomBalloon(n, PopupAnimation.Slide, 4000);
                    }
                        );
                    user_read_updates = false;
                }
            });




        }






        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show("Unhandled Exception. Please report the following details:\n" + e.ExceptionObject);
        }

        void AddShowClicked(object o)
        {
            AddShowFlyout.IsOpen = !AddShowFlyout.IsOpen;
        }

        private int lastpage = 0;
        private void SettingsClicked(object o)
        {
            if (CurrentPage() == 3)
            {
                SwitchPage(lastpage);
                return;
            }

            AddShowFlyout.IsOpen = false;
            FilterFlyout.IsOpen = false;
            lastpage = CurrentPage();
            SwitchPage(3);
        }

        private void IconClicked(object obj)
        {
            if (!IsVisible)
            {
                Show();
            }
            else
            {
                Activate();
            }
        }



        private void DownloadCommandExecute(string s)
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    Clipboard.SetText(s);
                    Clipboard.Flush();
                    return;
                }
                catch { }
                System.Threading.Thread.Sleep(10);
            }
            MessageBox.Show("Couldn't Copy link to clipboard.\n" + s);
        }

        private Accent currentAccent;
        public String CurrentAccent
        {
            get { return currentAccent.Name; }
            set
            {
                currentAccent = ThemeManager.DefaultAccents.First(x => x.Name == value);
                ThemeManager.ChangeTheme(this, currentAccent, Theme.Dark);
                setti.ThemeAccent = currentAccent.Name;
            }
        }

        public ICommand AddShowCommand { get; private set; }
        public ICommand SettingsCommand { get; private set; }
        public ICommand EpisodeClickedCommand { get; private set; }
        public ICommand ShowClickedCommand { get; private set; }
        public ICommand DownloadCommand { get; private set; }
        public ICommand IconClickedCommand { get; private set; }
        public ICommand TerminateCommand { get; private set; }

        int CurrentPage()
        {
            int i = 0;
            foreach (object child in MainGrid.Children)
            {
                Grid g = child as Grid;
                if (g.Visibility == Visibility.Visible)
                {
                    return i;
                }
                i++;
            }
            return -1;
        }

        void SwitchPage(int page)
        {
            int i = 0;
            foreach (object child in MainGrid.Children)
            {
                Grid g = child as Grid;
                if (g.Visibility ==Visibility.Visible)
                {
                    g.Visibility = Visibility.Collapsed;
                }
                if (i++ == page)
                {
                    g.Visibility = Visibility.Visible;
                }

            }

        }

        private IWorkItemResult currentWorkItem;
        private string nextSearchString;

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchString = TextBoxAutoComl.Text;

            if (currentWorkItem == null || currentWorkItem.IsCompleted)
            {
                currentWorkItem = StaticInstance.SmartThreadPool.QueueWorkItem(UpdateShowSearch, searchString);
            }
            else
            {
                nextSearchString = searchString;
            }
        }

        private void UpdateShowSearch(string query)
        {
            List<KeyValuePair<string, string>> result = SjInfo.SearchSjOrg(query);

            Dispatcher.Invoke(() =>
            {
                ListViewAutoCompl.ItemsSource =
                    result;
            });

            if (!string.IsNullOrEmpty(nextSearchString) && nextSearchString != query)
            {
                currentWorkItem = StaticInstance.SmartThreadPool.QueueWorkItem(UpdateShowSearch, nextSearchString);
            }
        }

        private void OnEpisodeViewClicked(EpisodeViewModel episodeView)
        {
         
            EpisodeGrid.DataContext = episodeView;
            SwitchPage(2);
            //EpisodeDataList.ItemsSource = episodeView.EpisodeDescriptors;
            //EpisodeDataLabel.Content = episodeView.DetailTitle;
        }


        void on_ShowViewClicked(object sender, ShowViewModel showView)
        {
            if(!IsVisible)
                Show();
            user_read_updates = true;
            AddShowFlyout.IsOpen = false;
            FilterFlyout.IsOpen = false;
            OnShowViewClicked(showView);
        }
        private void OnShowViewClicked(ShowViewModel showView)
        {
            ShowGrid.DataContext = showView;
            FilterFlyout.DataContext = showView;
            //Panorama.ItemsSource = showView.EpisodeGroups;
           // SeasonLabel.Content = showView.Title;
            SwitchPage(1);
        }

        private void ShowDelete(object sender, RoutedEventArgs e)
        {
            setti.TvShows.Remove((ShowGrid.DataContext as ShowViewModel).Show);
            SwitchPage(0);
        }

        private void EpisodeDataBack(object sender, RoutedEventArgs e)
        {
            SwitchPage(1);
        }
        private void EpisodesBack(object sender, RoutedEventArgs e)
        {
            SwitchPage(0);
            ((ShowViewModel) ShowGrid.DataContext).Show.NewEpisodes = false;
        }


        private void NavBack(object sender, ExecutedRoutedEventArgs e)
        {
            AddShowFlyout.IsOpen = false;
            FilterFlyout.IsOpen = false;
            switch (CurrentPage())
            {
                case 1:
                    EpisodesBack(this, new RoutedEventArgs());
                    break;
                case 2:
                    EpisodeDataBack(this, new RoutedEventArgs());
                    break;
                case 3:
                    SwitchPage(lastpage);
                    break;

            }
        }

        private void ListViewAutoCompl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ListViewAutoCompl.SelectedValue != null)
            {
                var selectedShow = (KeyValuePair<string,string>)ListViewAutoCompl.SelectedItem;

                for (int i = 0; i < setti.TvShows.Count; i++)
                {
                    if (setti.TvShows[i].Show.Url == selectedShow.Value)
                    {
                        return;
                    }
                }

                TextBoxAutoComl.Text = "";
                AddShowFlyout.IsOpen = false;
                //ShowView sv = new ShowView(selectedShow.Key, selectedShow.Value, true, Dispatcher);
                //showsCollection.Insert(showsCollection.Count() - 1,sv);
                setti.TvShows.Add(new FavShowData(new ShowData{Name=selectedShow.Key, Url=selectedShow.Value},true));
            }
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)

        {
            if (setti.MinimizeToTray)
            {
                e.Cancel = true;
                Hide();
                Settings.Save();
            }
            else
            {
                Settings.Save();
                if (Debugger.IsAttached)
                {
                    Environment.Exit(0);
                }
            }
           
   
            /*var fadeAnimation = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(0.6f)));
            fadeAnimation.AccelerationRatio = 0.2f;
            fadeAnimation.Completed += (Sender, Args) =>
                                       {
                                           Visibility = Visibility.Collapsed;
                                           Settings.Save();
                                           Environment.Exit(0);
                                       };
            e.Cancel = true;
            BeginAnimation(OpacityProperty, fadeAnimation);*/
        }


        private void Terminate(object obj)
        {
            Settings.Save();
            if (Debugger.IsAttached)
            {
                Environment.Exit(0);
            }
            else
            {
                Application.Current.Shutdown(0);
            }
        }


        private void ShowFilter(object sender, RoutedEventArgs e)
        {
            FilterFlyout.IsOpen = !FilterFlyout.IsOpen;
        }

    

        private void FilterFlyout_OnIsOpenChanged(object sender, EventArgs e)
        {
            if (!FilterFlyout.IsOpen)
            {
                var vm = FilterFlyout.DataContext as ShowViewModel;
                vm.Show.ApplyFilter();
            }
   
        }

    }
}
