using System;
using System.Dynamic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using MahApps.Metro;
using MahApps.Metro.Controls;

namespace SjUpdater.Updater
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class UpdateWindow : MetroWindow
    {
        private readonly Updater _updater;

        public delegate void UpdateStartetEventHandler(object sender, EventArgs e);

        public event UpdateStartetEventHandler UpdateStartedEvent;

        private readonly bool _restart;
        private readonly string _executable;
        private readonly string _parameter;

        private bool _shownOnce;
        private bool _myThemeChangeEvent;

        public UpdateWindow(string updateurl, bool restart = false, string exectuable = "", string parameter = "")
        {
            _restart = restart;
            _executable = exectuable;
            _parameter = parameter;

            ThemeManager.IsThemeChanged += ThemeManager_IsThemeChanged;
            var mainThemeSettings = ThemeManager.DetectAppStyle(Application.Current);
            ThemeManager.ChangeAppStyle(this,mainThemeSettings.Item2,ThemeManager.GetInverseAppTheme(mainThemeSettings.Item1));

            InitializeComponent();
            _updater = new Updater(updateurl);
            _updater.errorEvent += updater_errorEvent;

            DataContext = new UpdaterViewModel(ref _updater);
        }

        void ThemeManager_IsThemeChanged(object sender, OnThemeChangedEventArgs e)
        {
            if (_myThemeChangeEvent)
            {
                _myThemeChangeEvent = false;
                return;
            }
            _myThemeChangeEvent = true;
            ThemeManager.ChangeAppStyle(this,e.Accent, ThemeManager.GetInverseAppTheme(e.AppTheme));
        }

        private bool _forceClose;
        public bool TryClose()
        {
            if (_updater.IsUpdating)
                return false;

            if (!_shownOnce)
                Show(); //stupid workaround to correctly dispose window

            _forceClose = true;
            Close();
            return true;
        }

        private bool _error;
        void updater_errorEvent(object sender, System.IO.ErrorEventArgs e)
        {
            _error = true;
            MessageBox.Show(e.GetException().Message, "Updater Error");
            Close();
        }

        public void Show(bool showIfNoUpdateAvailable, bool silentCheck)
        {
            if (IsVisible)
            {
                Activate();
                return;
            }

            if (!silentCheck)
                Show();

            _updater.GetChangelog();

            var updaterTask = _updater.CheckForUpdates();

            if (silentCheck)
                updaterTask.ContinueWith(t =>
                                         {
                                             if (!_updater.UpdateAvailable && !showIfNoUpdateAvailable)
                                             {
                                                 return;
                                             }

                                             Dispatcher.Invoke(() =>
                                                               {
                                                                   if (!_error)
                                                                   {
                                                                       if (!_error)
                                                                           Show();
                                                                   }
                                                               });
                                         });
        }

        public new void Show()
        {
            _shownOnce = true;
            base.Show();
        }

        private void UpdateWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ((Grid) Content).Children.Remove(changelogGrid);
            ((Grid) Content).Children.Remove(updateGrid);

            TransitioningContentControl.Content = changelogGrid;
        }

        private void SwitchToUpdateGridButton_OnClick(object sender, RoutedEventArgs e)
        {
            ((Grid) Content).Children.Remove(changelogGrid);
            TransitioningContentControl.Content = updateGrid;

            var heightAnimation = new DoubleAnimation(Height, updateGrid.Height + 50, new Duration(TimeSpan.FromSeconds(1)));
            heightAnimation.AccelerationRatio = 0.2f;
            heightAnimation.EasingFunction = new CubicEase();

            SizeToContent = SizeToContent.Manual;

            BeginAnimation(HeightProperty, heightAnimation);

            var doUpdateTask = _updater.DoUpdate(_restart, _executable, _parameter);

            if (UpdateStartedEvent != null)
                UpdateStartedEvent(this, null);
        }

        private void UpdateWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_forceClose)
            {
                e.Cancel = true;
                Hide();
            }
        }
    }
}
