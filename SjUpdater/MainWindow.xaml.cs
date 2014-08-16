using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading;
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
using Timer = System.Timers.Timer;

namespace SjUpdater
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly Settings _setti;
        private readonly MainWindowViewModel _viewModel;
        public MainWindow()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            Application.Current.SessionEnding += Current_SessionEnding;

            DownloadCommand = new SimpleCommand<object, String>(DownloadCommandExecute);
            EpisodeClickedCommand = new SimpleCommand<object, EpisodeViewModel>(OnEpisodeViewClicked);
            ShowClickedCommand = new SimpleCommand<object, ShowViewModel>(OnShowViewClicked);
            SettingsCommand = new SimpleCommand<object, object>(SettingsClicked);
            IconClickedCommand = new SimpleCommand<object, object>(IconClicked);
            TerminateCommand = new SimpleCommand<object, object>(Terminate);

            _setti = Settings.Instance;
            _currentAccent = ThemeManager.GetAccent(_setti.ThemeAccent);
            _currentTheme = ThemeManager.GetAppTheme(_setti.ThemeBase);


            InitializeComponent();

            CurrentAccent = _setti.ThemeAccent;

            if (_setti.UpdateTime > 0) //Autoupdate enabled
            {
                var t = new Timer(_setti.UpdateTime);
                t.Elapsed += t_Elapsed;
                t.Start();
            }

            Update();

            _viewModel = new MainWindowViewModel(_setti.TvShows);
            ShowsPanorama.ItemsSource = _viewModel.PanoramaItems;
            SwitchPage(0);

            if (Environment.GetCommandLineArgs().Contains("-nogui"))
            {
               Hide();
            }

           StaticInstance.SmartThreadPool.QueueWorkItem(() => Stats.SendStats(!_setti.NoPersonalData));//Todo: Make Optional?
        }

        void t_Elapsed(object sender, ElapsedEventArgs e)
        {
            Update();
        }

        private void ReloadAll(object sender, RoutedEventArgs e)
        {
            Update();
        }

        private readonly Semaphore _sema = new Semaphore(1,1);

        private void Update()
        {
            if (!_sema.WaitOne(10)) return;

            var stp = new SmartThreadPool();
            stp.MaxThreads = (int) _setti.NumFetchThreads;
            var results = new List<IWaitableResult>();
            foreach (FavShowData t in _setti.TvShows)
            {
                results.Add(stp.QueueWorkItem(data => data.Fetch(), t)); //schedule update of show (executed paralell)
            }
            //wait for completion
            stp.QueueWorkItem(() =>
            {
                SmartThreadPool.WaitAll(results.ToArray());
                _sema.Release();

                var updates = _setti.TvShows.Where(show => show.NewEpisodes && !show.Notified).ToList();
                if (updates.Count > 0)
                {
                    updates.ForEach(show => show.Notified=true);
                    Dispatcher.Invoke(() =>
                    {
                        var n = new NotificationBalloon(updates);
                        n.ShowViewClicked += on_ShowViewClicked;
                        NotifyIcon.ShowCustomBalloon(n, PopupAnimation.Slide, 4000);
                    });
                }
            });

        }






        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show("Unhandled Exception. Please report the following details:\n" + e.ExceptionObject);
        }


        private void AddShow(object sender, RoutedEventArgs e)
        {
            AddShowFlyout.IsOpen = true;
        }   

        private int _lastpage = 0;
        private void SettingsClicked(object o)
        {
            if (CurrentPage() == 3)
            {
                SwitchPage(_lastpage);
                return;
            }

            SwitchPage(3);
        }

        private void IconClicked(object obj)
        {
            if (!IsVisible)
                Show();
            else
                Activate(); //"Bring to front"
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
                catch
                {
                    //nah
                }
                Thread.Sleep(10);
            }
            MessageBox.Show("Couldn't Copy link to clipboard.\n" + s);
        }

        private Accent _currentAccent;
        public String CurrentAccent
        {
            get { return _currentAccent.Name; }
            set
            {
                _currentAccent = ThemeManager.GetAccent(value);
                ThemeManager.ChangeAppStyle(Application.Current, _currentAccent,_currentTheme);
                _setti.ThemeAccent = _currentAccent.Name;
            }
        }

        private AppTheme _currentTheme;
        public String CurrentTheme
        {
            get { return _currentTheme.Name; }
            set
            {
                _currentTheme = ThemeManager.GetAppTheme(value);
                ThemeManager.ChangeAppStyle(Application.Current, _currentAccent, _currentTheme);
                _setti.ThemeBase = _currentTheme.Name;
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
                    _lastpage = i;
                }
                if (i++ == page)
                {
                    g.Visibility = Visibility.Visible;
                }

            }
            AddShowFlyout.IsOpen = false;
            FilterFlyout.IsOpen = false;

        }

        private IWorkItemResult _currentWorkItem;
        private string _nextSearchString;

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchString = TextBoxAutoComl.Text;

            if (_currentWorkItem == null || _currentWorkItem.IsCompleted)
            {
                _currentWorkItem = StaticInstance.SmartThreadPool.QueueWorkItem(UpdateShowSearch, searchString);
            }
            else
            {
                _nextSearchString = searchString;
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

            if (!string.IsNullOrEmpty(_nextSearchString) && _nextSearchString != query)
            {
                _currentWorkItem = StaticInstance.SmartThreadPool.QueueWorkItem(UpdateShowSearch, _nextSearchString);
            }
        }

        private void OnEpisodeViewClicked(EpisodeViewModel episodeView)
        {
            EpisodeGrid.DataContext = episodeView;
            episodeView.Episode.NewEpisode = false;
            episodeView.Episode.NewUpdate = false;
            SwitchPage(2);
        }


        void on_ShowViewClicked(object sender, ShowViewModel showView)
        {
            if(!IsVisible)
                Show();
            showView.Show.NewEpisodes = false;
            OnShowViewClicked(showView);
        }
        private void OnShowViewClicked(ShowViewModel showView)
        {
            ShowGrid.DataContext = showView;
            FilterFlyout.DataContext = showView;
            SwitchPage(1);
        }

       

         private void ShowGotoPage(object sender, RoutedEventArgs e)
         {
             var vm = ((ShowViewModel) ShowGrid.DataContext).Show;
             var url = vm.Show.Url;
             var p = new Process();
             p.StartInfo = new ProcessStartInfo(url);
             p.Start();
         }

         private void OpenHomepage(object sender, RoutedEventArgs e)
         {
             var p = new Process();
             p.StartInfo = new ProcessStartInfo("http://serienjunkies.org");
             p.Start();
         }


        private void ShowDelete(object sender, RoutedEventArgs e)
        {
            _setti.TvShows.Remove(((ShowViewModel) ShowGrid.DataContext).Show);
            SwitchPage(0);
        }

        private void EpisodeDataBack(object sender, RoutedEventArgs e)
        {
            SwitchPage(1);
        }
        private void EpisodesBack(object sender, RoutedEventArgs e)
        {
            SwitchPage(0);
        }


        private void NavBack(object sender, ExecutedRoutedEventArgs e)
        {
            switch (CurrentPage())
            {
                case 1:
                    EpisodesBack(this, new RoutedEventArgs());
                    break;
                case 2:
                    EpisodeDataBack(this, new RoutedEventArgs());
                    break;
                case 3:
                    SwitchPage(_lastpage);
                    break;

            }
        }

        private void ListViewAutoCompl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ListViewAutoCompl.SelectedValue != null)
            {
                var selectedShow = (KeyValuePair<string,string>)ListViewAutoCompl.SelectedItem;

                if (_setti.TvShows.Any(t => t.Show.Url == selectedShow.Value))
                {
                    return;
                }

                TextBoxAutoComl.Text = "";
                AddShowFlyout.IsOpen = false;
                _setti.TvShows.Add(new FavShowData(new ShowData{Name=selectedShow.Key, Url=selectedShow.Value},true));
            }
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)

        {
            if (_setti.MinimizeToTray)
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
        }


        private void Terminate(object obj)
        {
            Settings.Save();
             if (Debugger.IsAttached)
                Environment.Exit(0);
            else
                Application.Current.Shutdown(0);
        }

        void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Settings.Save();
        }
        
        void Current_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            Settings.Save();
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

        public static readonly Dictionary<String, int> DictUpdateTimes = new Dictionary<string, int>()
        {
            {"Never", -1},
            {"5 min", 1000*60*5},
            {"15 min",  1000*60*15},
            {"30 min",  1000*60*30},
            {"1 h",  1000*3600*1},
            {"2 h",  1000*3600*2},
            {"3 h",  1000*3600*3},
            {"6 h",  1000*3600*6},
            {"12 h", 1000*3600*12},
            {"24 h", 1000*3600*24}
        };

    }
}
