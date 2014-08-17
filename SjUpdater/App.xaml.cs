using System;
using System.Windows;
using SjUpdater.Utils;

namespace SjUpdater
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
           
            if (!GlobalMutex.TryGetMutex()) {
                Environment.Exit(0);
            } else { 
                base.OnStartup(e);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            GlobalMutex.ReleaseMutex();
            base.OnExit(e);
        }
    }
}
