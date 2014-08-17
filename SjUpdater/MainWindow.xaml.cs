using System.Reflection;
using Amib.Threading;
using MahApps.Metro;
using SjUpdater.Model;
using SjUpdater.Updater;
using SjUpdater.Utils;
using SjUpdater.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
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
        private readonly UpdateWindow _updater;

        public MainWindow()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.Current.SessionEnding += Current_SessionEnding;

            //Commands
            DownloadCommand = new SimpleCommand<object, String>(DownloadCommandExecute);
            EpisodeClickedCommand = new SimpleCommand<object, EpisodeViewModel>(OnEpisodeViewClicked);
            ShowClickedCommand = new SimpleCommand<object, ShowViewModel>(OnShowViewClicked);
            SettingsCommand = new SimpleCommand<object, object>(SettingsClicked);
            IconClickedCommand = new SimpleCommand<object, object>(IconClicked);
            TerminateCommand = new SimpleCommand<object, object>(Terminate);

            //Settings & Theme
            _setti = Settings.Instance;
            _currentAccent = ThemeManager.GetAccent(_setti.ThemeAccent);
            _currentTheme = ThemeManager.GetAppTheme(_setti.ThemeBase);
            CurrentAccent = _setti.ThemeAccent;

            //Updater
            _updater = new UpdateWindow("http://sjupdater.batrick.de/updater/latest", true, "SjUpdater.exe", "");
            _updater.updateStartedEvent += (a, dsa) => Terminate(null);

            //Start!
            InitializeComponent();
            if (Environment.GetCommandLineArgs().Contains("-nogui"))
                Hide();

            //Initialize view
            _viewModel = new MainWindowViewModel(_setti.TvShows);
            ShowsPanorama.ItemsSource = _viewModel.PanoramaItems;
            SwitchPage(0);
    
            //Autoupdate timer
            if (_setti.UpdateTime > 0) //Autoupdate enabled
            {
                var t = new Timer(_setti.UpdateTime);
                t.Elapsed += t_Elapsed;
                t.Start();
            }

            //Inital update
            Update();

            //Stats
            StaticInstance.SmartThreadPool.QueueWorkItem(() => Stats.SendStats(!_setti.NoPersonalData));
        }

        private void t_Elapsed(object sender, ElapsedEventArgs e)
        {
            Update();
        }

        private void ReloadAll(object sender, RoutedEventArgs e)
        {
            Update();
        }

        private readonly Semaphore _sema = new Semaphore(1, 1);

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
                                      updates.ForEach(show => show.Notified = true);
                                      Dispatcher.Invoke(() =>
                                                        {
                                                            var n = new NotificationBalloon(updates);
                                                            n.ShowViewClicked += on_ShowViewClicked;
                                                            NotifyIcon.ShowCustomBalloon(n, PopupAnimation.Slide, 4000);
                                                        });
                                  }
                              });

        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
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
                ThemeManager.ChangeAppStyle(Application.Current, _currentAccent, _currentTheme);
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

        public String CurrentVersionString
        {
            get { return Stats.GetVersionString(); }
        }

        public ICommand AddShowCommand { get; private set; }
        public ICommand SettingsCommand { get; private set; }
        public ICommand EpisodeClickedCommand { get; private set; }
        public ICommand ShowClickedCommand { get; private set; }
        public ICommand DownloadCommand { get; private set; }
        public ICommand IconClickedCommand { get; private set; }
        public ICommand TerminateCommand { get; private set; }

        private int CurrentPage()
        {
            return TabControl.SelectedIndex;
        }

        private void SwitchPage(int page)
        {
            _lastpage = TabControl.SelectedIndex;
            TabControl.SelectedIndex = page;
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
                                  ListViewAutoCompl.ItemsSource = result;
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


        private void on_ShowViewClicked(object sender, ShowViewModel showView)
        {
            if (!IsVisible)
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
                var selectedShow = (KeyValuePair<string, string>) ListViewAutoCompl.SelectedItem;

                if (_setti.TvShows.Any(t => t.Show.Url == selectedShow.Value))
                {
                    return;
                }

                TextBoxAutoComl.Text = "";
                AddShowFlyout.IsOpen = false;
                _setti.TvShows.Add(new FavShowData(new ShowData {Name = selectedShow.Key, Url = selectedShow.Value}, true));
            }
        }

        private bool _forceClose = false;

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (!_forceClose && _setti.MinimizeToTray)
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                _updater.TryClose();
                NotifyIcon.Dispose();
                Settings.Save();
            }
        }

        /// <summary>
        /// Terminates the application instead of minimizing
        /// </summary>
        /// <param name="obj"></param>
        private void Terminate(object obj)
        {
            _forceClose = true;
            Close();
            
        }

        //called when windows shuts down or user logs out
        private void Current_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            Terminate(null);
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
                                                                             {"5 min", 1000 * 60 * 5},
                                                                             {"15 min", 1000 * 60 * 15},
                                                                             {"30 min", 1000 * 60 * 30},
                                                                             {"1 h", 1000 * 3600 * 1},
                                                                             {"2 h", 1000 * 3600 * 2},
                                                                             {"3 h", 1000 * 3600 * 3},
                                                                             {"6 h", 1000 * 3600 * 6},
                                                                             {"12 h", 1000 * 3600 * 12},
                                                                             {"24 h", 1000 * 3600 * 24}
                                                                         };

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Timer timer = new Timer(3000);
            timer.AutoReset = false;
            timer.Elapsed += (o, args) => Dispatcher.Invoke(() => _updater.Show(false, true));

            timer.Start();
        }

        private void ChangeLogButtonClicked(object sender, RoutedEventArgs e)
        {
            _updater.Show(true,false);
        }

    }
}
