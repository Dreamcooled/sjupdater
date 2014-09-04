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
        private Updater updater;

        public delegate void UpdateStartetEventHandler(object sender, EventArgs e);

        public event UpdateStartetEventHandler updateStartedEvent;

        private readonly bool restart;
        private readonly string executable;
        private readonly string parameter;

        private bool shownOnce = false;
        private bool myThemeChangeEvent = false;

        public UpdateWindow(string updateurl, bool restart = false, string exectuable = "", string parameter = "")
        {
            this.restart = restart;
            this.executable = exectuable;
            this.parameter = parameter;

            ThemeManager.IsThemeChanged += ThemeManager_IsThemeChanged;
            var mainThemeSettings = ThemeManager.DetectAppStyle(Application.Current);
            ThemeManager.ChangeAppStyle(this,mainThemeSettings.Item2,ThemeManager.GetInverseAppTheme(mainThemeSettings.Item1));

            InitializeComponent();
            updater = new Updater(updateurl);
            updater.errorEvent += updater_errorEvent;

            this.DataContext = new UpdaterViewModel(ref updater);

            //Timeline.DesiredFrameRateProperty.OverrideMetadata(
            //   typeof(Timeline),
            //   new FrameworkPropertyMetadata { DefaultValue = 1 }
            //   );
        }

        void ThemeManager_IsThemeChanged(object sender, OnThemeChangedEventArgs e)
        {
            if (myThemeChangeEvent)
            {
                myThemeChangeEvent = false;
                return;
            }
            myThemeChangeEvent = true;
            ThemeManager.ChangeAppStyle(this,e.Accent, ThemeManager.GetInverseAppTheme(e.AppTheme));
        }

        private bool force_close = false;
        public bool TryClose()
        {
            if (updater.IsUpdating)
                return false;

            if (!shownOnce)
                this.Show(); //stupid workaround to correctly dispose window

            force_close = true;
            this.Close();
            return true;
        }

        private bool error;
        void updater_errorEvent(object sender, System.IO.ErrorEventArgs e)
        {
            error = true;
            MessageBox.Show(e.GetException().Message, "Updater Error");
            this.Close();
        }

        public void Show(bool ShowIfNoUpdateAvailable, bool SilentCheck)
        {
            if (IsVisible)
            {
                this.Activate();
                return;
            }

            if (!SilentCheck)
                this.Show();

            updater.GetChangelog();

            var updaterTask = updater.CheckForUpdates();

            if (SilentCheck)
                updaterTask.ContinueWith(t =>
                                         {
                                             if (!updater.UpdateAvailable && !ShowIfNoUpdateAvailable)
                                             {
                                                 return;
                                             }

                                             Dispatcher.Invoke(() =>
                                                               {
                                                                   if (!error)
                                                                   {
                                                                       if (!error)
                                                                           this.Show();
                                                                   }
                                                               });
                                         });
        }

        public new void Show()
        {
            shownOnce = true;
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

            DoubleAnimation heightAnimation = new DoubleAnimation(this.Height, updateGrid.Height + 50, new Duration(TimeSpan.FromSeconds(1)));
            heightAnimation.AccelerationRatio = 0.2f;
            heightAnimation.EasingFunction = new CubicEase();

            this.SizeToContent = SizeToContent.Manual;

            this.BeginAnimation(FrameworkElement.HeightProperty, heightAnimation);

            var doUpdateTask = updater.DoUpdate(restart, executable, parameter);

            if (updateStartedEvent != null)
                updateStartedEvent(this, null);
        }

        private void UpdateWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!force_close)
            {
                e.Cancel = true;
                this.Hide();
            }
        }
    }
}
