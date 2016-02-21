using System.IO;
using System.Reflection;
using Amib.Threading;
using MahApps.Metro;
using MahApps.Metro.Controls.Dialogs;
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
        private readonly Timer _updateTimer;

        private readonly List<object> _selectedEpisodeTreeItems = new List<object>();

        public MainWindow()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.Current.SessionEnding += Current_SessionEnding;

            //Commands
            ShowClickedCommand = new SimpleCommand<object, ShowTileViewModel>(OnShowViewClicked);
            IconClickedCommand = new SimpleCommand<object, object>(IconClicked);
            TerminateCommand = new SimpleCommand<object, object>(Terminate);

            //Settings & Theme
            _setti = Settings.Instance;

            _currentAccent = ThemeManager.GetAccent(_setti.ThemeAccent);
            _currentTheme = ThemeManager.GetAppTheme(_setti.ThemeBase);
            CurrentAccent = _setti.ThemeAccent;

            _updateTimer = new Timer();
            //Updater
            _updater = new UpdateWindow("https://dreamcooled.github.io/sjupdater/latest", true, "SjUpdater.exe", "-uf " + Stats.GetVersionString());
            _updater.UpdateStartedEvent += (a, dsa) =>
            {
                Terminate(null);
                Stats.TrackAction(Stats.TrackActivity.AppUpdate);
            };

            //Start!
            InitializeComponent();
            if (Environment.GetCommandLineArgs().Contains("-nogui"))
                Hide();

            //Initialize view
            _viewModel = new MainWindowViewModel(_setti.TvShows);
            DataContext = _viewModel;

            //Enhance TreeView with Multiselection Extension
            //Note: We could also pass a observable collection to the first method, and get changes from the CollectionChanged Event on the observablecollection
            //But this way (custom event) we get less Events, which speeds up the GUI
            TreeViewExtensions.SetSelectedItems(ShowTreeView, _selectedEpisodeTreeItems);
            TreeViewExtensions.AddSelectionChangedListener(ShowTreeView, _selectedEpisodeTreeItems_CollectionChanged);

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

            Stats.StatsUrl = "https://sjupdater.batrick.de/stats";
            Stats.AllowCustom = !_setti.NoPersonalData;
            Stats.TrackAction(Stats.TrackActivity.AppStart);
            //Stats.TrackCustomVariable("Shows", _setti.TvShows.Select(s => s.Name)); //useless, as we don't even do anything with that data

            if (_setti.CheckForUpdates)
            {
                _updater.Show(false, true);
                _updateTimer = new Timer(1000 * 60 * 30); // 30 minutes
                _updateTimer.Elapsed += (o, args) => Dispatcher.Invoke(() => _updater.Show(false, true));
                _updateTimer.Start();
            }
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

            var stp = StaticInstance.ThreadPool;
            //stp.MaxThreads = (int) _setti.NumFetchThreads;
            var results = new List<IWaitableResult>();
            foreach (var t in _setti.TvShows)
            {
                results.Add(stp.QueueWorkItem(data => data.Fetch(), t, true, WorkItemPriority.BelowNormal)); //schedule update of show (executed paralell)
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

                    if (_setti.ShowNotifications)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            var n = new NotificationBalloon(updates);
                            n.ShowViewClicked += on_ShowViewClicked;

                            NotifyIcon.ShowCustomBalloon(n, PopupAnimation.Slide, _setti.NotificationTimeout <= 0 ? (int?)null : _setti.NotificationTimeout);
                        });
                    }
                }
            }, true, WorkItemPriority.BelowNormal);
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

        private void SettingsClicked(object sender, RoutedEventArgs e)
        {
            if (CurrentPage() == 2)
            {
                SwitchPage(_lastpage);
                return;
            }

            SwitchPage(2);
        }

        private void IconClicked(object obj)
        {
            if (!IsVisible)
                Show();
            else
                Activate(); //"Bring to front"
        }

        private Accent _currentAccent;

        public string CurrentAccent
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

        public string CurrentTheme
        {
            get { return _currentTheme.Name; }
            set
            {
                _currentTheme = ThemeManager.GetAppTheme(value);
                ThemeManager.ChangeAppStyle(Application.Current, _currentAccent, _currentTheme);
                _setti.ThemeBase = _currentTheme.Name;
            }
        }

        public string CurrentVersionString
        {
            get { return Stats.GetVersionString(); }
        }

        public ICommand AddShowCommand { get; private set; }
        public ICommand ShowClickedCommand { get; private set; }
        public ICommand IconClickedCommand { get; private set; }
        public ICommand TerminateCommand { get; private set; }


        private int CurrentPage()
        {
            return MainTabControl.SelectedIndex;
        }

        private void SwitchPage(int page)
        {
            _lastpage = MainTabControl.SelectedIndex;
            MainTabControl.SelectedIndex = page;
            AddShowFlyout.IsOpen = false;
            FilterFlyout.IsOpen = false;
        }

        private IWorkItemResult _currentWorkItem;
        private string _nextSearchString;

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchString = TextBoxAutoComl.Text;

            if (_currentWorkItem == null || _currentWorkItem.IsCompleted)
            {
                _currentWorkItem = StaticInstance.ThreadPool.QueueWorkItem(UpdateShowSearch, searchString, true, WorkItemPriority.Highest);
            }
            else
            {
                _nextSearchString = searchString;
            }
        }

        private void UpdateShowSearch(string query)
        {
            var result = SjInfo.SearchSjOrg(query);

            Dispatcher.Invoke(() =>
            {
                ListViewAutoCompl.ItemsSource = result;
            });

            if (!string.IsNullOrEmpty(_nextSearchString) && _nextSearchString != query)
            {
                _currentWorkItem = StaticInstance.ThreadPool.QueueWorkItem(UpdateShowSearch, _nextSearchString, true, WorkItemPriority.Highest);
            }
        }

        private void on_ShowViewClicked(object sender, ShowTileViewModel showView)
        {
            if (!IsVisible)
                Show();
            OnShowViewClicked(showView);
        }

        private void OnShowViewClicked(ShowTileViewModel showTileView)
        {
            var showView = showTileView.ShowViewModel;
            _selectedEpisodeTreeItems.Clear();
            ShowGrid.DataContext = showView;
            FilterFlyout.DataContext = showView;
            SwitchPage(1);
        }

        private void ShowGotoPage(object sender, RoutedEventArgs e)
        {
            var vm = ((ShowViewModel)ShowGrid.DataContext).Show;
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

        private void GithubClicked(object sender, RoutedEventArgs e)
        {
            var p = new Process();
            p.StartInfo = new ProcessStartInfo("https://github.com/Dreamcooled/sjupdater");
            p.Start();
        }

        private async void ShowDelete(object sender, RoutedEventArgs e)
        {
            var res = await this.ShowMessageAsync("Remove Show?", "Do you really want to remove this show from your favorites?",
                MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings {AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateShow = false, AnimateHide = false});
            if (res == MessageDialogResult.Affirmative)
            {
                _setti.TvShows.Remove(((ShowViewModel)ShowGrid.DataContext).Show);
                SwitchPage(0);
            }
        }

        private readonly MultiSelectionViewModel _multiSelectionViewModel = new MultiSelectionViewModel();

        private void _selectedEpisodeTreeItems_CollectionChanged(object sender)
        {
            var first = _selectedEpisodeTreeItems.FirstOrDefault();
            if (_selectedEpisodeTreeItems.Count == 1 && first is SeasonViewModel)
            {
                EpisodeTabControl_Season.DataContext = first;
                EpisodeTabControl.SelectedIndex = 1;
            }
            else if (_selectedEpisodeTreeItems.Count == 1 && first is EpisodeViewModel)
            {
                var vm = (first as EpisodeViewModel);
                vm.Episode.NewEpisode = false;
                vm.Episode.NewUpdate = false;
                EpisodeTabControl_Episode.DataContext = first;
                EpisodeTabControl.SelectedIndex = 2;
            }
            else if (!_selectedEpisodeTreeItems.Any())
            {
                EpisodeTabControl_Episode.DataContext = _selectedEpisodeTreeItems;
                EpisodeTabControl.SelectedIndex = 0;
            }
            else
            {
                var selectedEpisodes = _selectedEpisodeTreeItems.OfType<EpisodeViewModel>().Select(ev => ev.Episode).ToList();
                _multiSelectionViewModel.SelectedEpisodes = selectedEpisodes;
                EpisodeTabControl_Multi.DataContext = _multiSelectionViewModel;
                EpisodeTabControl.SelectedIndex = 3;
            }
        }

        private void CleanShow(object sender, RoutedEventArgs e)
        {
            var s = ((ShowViewModel)ShowGrid.DataContext).Show;
            StaticInstance.ThreadPool.QueueWorkItem(() => s.ApplyFilter(true, false), true, WorkItemPriority.AboveNormal);
        }

        private void EpisodesBack(object sender, RoutedEventArgs e)
        {
            //The following block is a bit hacky. but still easier than to subscribe on the changed events of every episode
            var show = ((ShowViewModel)ShowGrid.DataContext).Show;
            show.NewEpisodes = show.Seasons.Any(s => s.Episodes.Any(ep => ep.NewEpisode));
            show.NewUpdates = show.Seasons.Any(s => s.Episodes.Any(ep => ep.NewUpdate));
            show.NotifyBigChange();
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
                    SwitchPage(_lastpage);
                    break;

            }
        }

        private void ListViewAutoCompl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            AddSelectedShow();
        }

        private void TextBoxAutoComl_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                AddSelectedShow();
        }

        private void AddSelectedShowButton_Click(object sender, RoutedEventArgs e)
        {
            AddSelectedShow();
        }

        private void AddSelectedShow()
        {
            if (ListViewAutoCompl.HasItems && ListViewAutoCompl.SelectedItem == null)
                ListViewAutoCompl.SelectedIndex = 0;

            if (ListViewAutoCompl.SelectedItem != null)
            {
                foreach (var item in ListViewAutoCompl.SelectedItems)
                {
                    var selectedShow = (KeyValuePair<string, string>)item;

                    if (_setti.TvShows.Any(t => t.Show.Url == selectedShow.Value))
                    {
                        return;
                    }

                    TextBoxAutoComl.Text = "";
                    AddShowFlyout.IsOpen = false;
                    _setti.TvShows.Add(new FavShowData(new ShowData {Name = selectedShow.Key, Url = selectedShow.Value}, true));
                }
            }
        }

        private bool _forceClose = false;

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (!_forceClose && _setti.MinimizeToTray && !Debugger.IsAttached)
            {
                e.Cancel = true; //abort closing
                Hide(); // hide instead
                Settings.Save(); // ... and save the config 
            }
            else
            {
                Settings.Save(); //do the important stuff first!
                _updater.TryClose();
                NotifyIcon.Dispose();
                Stats.TrackAction(Stats.TrackActivity.AppTerm);
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
                vm.Show.ApplyFilter(true, false);
                var firstSeason = vm.Seasons.FirstOrDefault();
                if (firstSeason != null) firstSeason.IsExpanded = true;
            }
        }

        public static readonly Dictionary<string, int> DictUpdateTimes = new Dictionary<string, int>()
        {
            {"Never", - 1},
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

        public static readonly Dictionary<string, int> DictNotifyTimeouts = new Dictionary<string, int>()
        {
            {"Never", - 1},
            {"2 sec", 1000 * 2},
            {"3 sec", 1000 * 3},
            {"5 sec", 1000 * 5},
            {"10 sec", 1000 * 10},
            {"20 sec", 1000 * 20},
            {"30 sec", 1000 * 30},
            {"1 m", 1000 * 60},
            {"2 m", 1000 * 60 * 2},
            {"3 m", 1000 * 60 * 3},
            {"5 m", 1000 * 60 * 5}
        };

        private void StatsInfoButtonClicked(object sender, RoutedEventArgs e)
        {
            this.ShowMessageAsync("Info about Stats", Stats.GetInfoForUser());
        }

        private void ChangeLogButtonClicked(object sender, RoutedEventArgs e)
        {
            _updater.Show(true, false);
        }

        private void restartButton_Click(object sender, RoutedEventArgs e)
        {
            var exectuable = "SjUpdater.exe";
            var parameter = "";
            var command =
                "/C @echo off & for /l %a in (0) do TaskList /FI \"IMAGENAME eq " + exectuable + "\" 2>NUL | Find \"" + exectuable + "\" >NUL || " + //Waits on app termination
                "( start " + exectuable + " " + parameter + " & EXIT)";
            var psi = new ProcessStartInfo("cmd.exe", command);
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var cmd = new Process();
            cmd.StartInfo = psi;
            cmd.Start();
            Terminate(null);
        }

        private void MarkContextMenuButtonClicked(object sender, RoutedEventArgs e)
        {
            var em = (e.Source as FrameworkElement);
            em.ContextMenu.PlacementTarget = em;
            em.ContextMenu.IsOpen = true;
        }


        private void OutsideTreeview_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            TreeViewExtensions.ClearSelection(ShowTreeView);
        }

        private void EpisodeShowAllDownloads(object sender, RoutedEventArgs e)
        {
            EpisodePopup.IsOpen = true;
        }

        private void EpisodeCloseAllDownloads(object sender, RoutedEventArgs e)
        {
            EpisodePopup.IsOpen = false;
        }

        private void EpisodePopup_Closed(object sender, EventArgs e)
        {
            EpisodeFavorizedUploadsListView.GetBindingExpression(ItemsControl.ItemsSourceProperty).UpdateTarget();
            EpisodeFavorizedWarning1.GetBindingExpression(VisibilityProperty).UpdateTarget();
            EpisodeFavorizedWarning2.GetBindingExpression(VisibilityProperty).UpdateTarget();
        }


        private void SeasonShowAllDownloads(object sender, RoutedEventArgs e)
        {
            SeasonPopup.IsOpen = true;
        }

        private void SeasonCloseAllDownloads(object sender, RoutedEventArgs e)
        {
            SeasonPopup.IsOpen = false;
        }

        private void NoneShowAllDownloads(object sender, RoutedEventArgs e)
        {
            NonePopup.IsOpen = true;
        }

        private void NoneCloseAllDownloads(object sender, RoutedEventArgs e)
        {
            NonePopup.IsOpen = false;
        }

        private void CategorySettingsButtonUp_OnClick(object sender, RoutedEventArgs e)
        {
            var catSettings = _setti.CategorySettings;
            var selCatSetting = CategorySettingsDataGrid.SelectedItem as ShowCategorySettings;
            if (selCatSetting == null) return;
            if (CategorySettingsDataGrid.SelectedIndex > 0)
            {
                catSettings.Move(CategorySettingsDataGrid.SelectedIndex, CategorySettingsDataGrid.SelectedIndex-1);
            }
        }

        private void CategorySettingsButtonDown_OnClick(object sender, RoutedEventArgs e)
        {
            var catSettings = _setti.CategorySettings;
            var selCatSetting = CategorySettingsDataGrid.SelectedItem as ShowCategorySettings;
            if (selCatSetting == null) return;
            if (CategorySettingsDataGrid.SelectedIndex < catSettings.Count -1)
            {
                catSettings.Move(CategorySettingsDataGrid.SelectedIndex, CategorySettingsDataGrid.SelectedIndex + 1);
            }
        }
    }
}
