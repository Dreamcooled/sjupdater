using System;
using System.IO;
using System.Net;
using System.Reflection;
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
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            if (!GlobalMutex.TryGetMutex()) {
                Environment.Exit(0);
            } else { 
                base.OnStartup(e);
            }

            ServicePointManager.DefaultConnectionLimit = 25;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            GlobalMutex.ReleaseMutex();
            base.OnExit(e);
        }
    }
}
