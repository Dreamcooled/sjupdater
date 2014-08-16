using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace SjUpdater.Updater
{
    internal class UpdaterViewModel : INotifyPropertyChanged
    {
        private Updater updater;

        public UpdaterViewModel(ref Updater updater)
        {
            this.updater = updater;

            updater.PropertyChanged += updater_PropertyChanged;
        }

        private void updater_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "CurrentFileNum":
                case "NumFiles":
                    NotifyPropertyChanged("FileNumProgress");
                    break;

                case "TotalBytesDownloaded":
                case "TotalBytes":
                    NotifyPropertyChanged("TotalDownloadProgressBytes");
                    NotifyPropertyChanged("TotalDownloadProgressPercentageString");
                    NotifyPropertyChanged("TotalDownloadProgressPercentageFloat");
                    break;

                case "CurrentFileBytesDownloaded":
                case "CurrentFileBytes":
                    NotifyPropertyChanged("CurrentDownloadProgressBytes");
                    NotifyPropertyChanged("CurrentDownloadProgressPercentageString");
                    NotifyPropertyChanged("CurrentDownloadProgressPercentageFloat");
                    break;

                case "CurrentFilename":
                    NotifyPropertyChanged("CurrentFilename");
                    break;

                case "UpdateAvailable":
                case "IsChecking":
                case "IsGettingChangelog":
                    NotifyPropertyChanged("IsButtonEnabled");
                    NotifyPropertyChanged("UpdateButtonContent");
                    NotifyPropertyChanged("IsWorking");
                    NotifyPropertyChanged("ProgressbarVisibility");
                    break;

                case "Changelog":
                    NotifyPropertyChanged("Changelog");
                    break;
            }
        }

        private static string ReadableFileSize(double size, int unit = 0)
        {
            string[] units = {"B", "KB", "MB", "GB"};

            while (size >= 1000)
            {
                size /= 1000;
                ++unit;
            }

            return string.Format("{0:0.00} {1}", size, units[unit]);
        }

        public string CurrentFilename { get { return updater.CurrentFilename; } }

        public string FileNumProgress { get { return string.Format("File {0} / {1}", updater.CurrentFileNum, updater.NumFiles); } }

        public string TotalDownloadProgressBytes { get { return string.Format("{0} / {1}", ReadableFileSize(updater.TotalBytesDownloaded), ReadableFileSize(updater.TotalBytes)); } }

        public string TotalDownloadProgressPercentageString { get { return string.Format("{0:0.00}%", (float) updater.TotalBytesDownloaded / (float) updater.TotalBytes * 100f); } }

        public float TotalDownloadProgressPercentageFloat { get { return (float) updater.TotalBytesDownloaded / (float) updater.TotalBytes; } }

        public string CurrentDownloadProgressBytes { get { return string.Format("{0} / {1}", ReadableFileSize(updater.CurrentFileBytesDownloaded), ReadableFileSize(updater.CurrentFileBytes)); } }

        public string CurrentDownloadProgressPercentageString { get { return string.Format("{0:0.00} %", (float) updater.CurrentFileBytesDownloaded / (float) updater.CurrentFileBytes * 100f); } }

        public float CurrentDownloadProgressPercentageFloat { get { return (float) updater.CurrentFileBytesDownloaded / (float) updater.CurrentFileBytes; } }

        public bool IsButtonEnabled { get { return !IsWorking && updater.UpdateAvailable; } }

        public bool IsWorking { get { return updater.IsChecking | updater.IsGettingChangelog; } }

        public Visibility ProgressbarVisibility { get { return IsWorking ? Visibility.Visible : Visibility.Collapsed; } }

        public string UpdateButtonContent { get { return (IsWorking ? "Working..." : (updater.UpdateAvailable ? "Update!" : "No Update Available")); } }

        public string Changelog { get { return updater.Changelog; } }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}