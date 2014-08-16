using System;
using System.Dynamic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
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

        private bool restart;
        private string executable;
        private string parameter;

        private bool shownOnce = false;

        public UpdateWindow(string updateurl, bool restart = false, string exectuable = "", string parameter = "")
        {
            this.restart = restart;
            this.executable = exectuable;
            this.parameter = parameter;

            InitializeComponent();

            updater = new Updater(updateurl);
            updater.errorEvent += updater_errorEvent;

            this.DataContext = new UpdaterViewModel(ref updater);
        }

        public bool TryClose()
        {
            if (updater.IsUpdating)
                return false;

            if (!shownOnce)
                this.Show(); //stupid workaround to correctly dispose window

            this.Close();
            return true;
        }

        private bool error = false;
        void updater_errorEvent(object sender, System.IO.ErrorEventArgs e)
        {
            error = true;
            MessageBox.Show(e.GetException().Message, "Updater Error");
            this.Close();
        }

        public void Show(bool ShowIfNoUpdateAvailable, bool SilentCheck)
        {
            if (!SilentCheck)
                this.Show();

            var updaterTask = updater.CheckForUpdates();

            Task getChangelogTask = updater.GetChangelog();

            if(SilentCheck)
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
                                                                   if(!error)
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
            (this.Content as Grid).Children.Remove(changelogGrid);
            (this.Content as Grid).Children.Remove(updateGrid);

            TransitioningContentControl.Content = changelogGrid;
        }

        private void SwitchToUpdateGridButton_OnClick(object sender, RoutedEventArgs e)
        {
            (this.Content as Grid).Children.Remove(changelogGrid);
            TransitioningContentControl.Content = updateGrid;

            DoubleAnimation heightAnimation = new DoubleAnimation(this.Height, updateGrid.Height + 50, new Duration(TimeSpan.FromSeconds(1)));
            heightAnimation.AccelerationRatio = 0.2f;
            heightAnimation.EasingFunction = new CubicEase();

            this.SizeToContent = SizeToContent.Manual;

            this.BeginAnimation(MetroWindow.HeightProperty, heightAnimation);

            var doUpdateTask = updater.DoUpdate(restart, executable, parameter);

            if (updateStartedEvent != null)
                updateStartedEvent(this, null);
        }
    }
}
