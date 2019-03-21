using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Shell;
using ProjectLama.Properties;

namespace ProjectLama
{
    partial class App : Application, ISingleInstanceApp
    {
        private const string Unique = "My_Unique_Application_String";

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (!SingleInstance<App>.InitializeAsFirstInstance(Unique)) return;
            int file = 0,theme = 0;
            string path = null;
            var exist = false;      
            double vol = 1;
            try
            {
                if (Settings.Default != null)
                {
                    vol = Settings.Default.Volume;
                    theme = Settings.Default.Theme;
                    if (Settings.Default.Track.Capacity != 0)
                        exist = true;
                }
            }
            catch (Exception) { }
            if (e.Args.Length == 1)
            {
                if (e.Args[0].EndsWith(".lpl"))
                {
                    path = e.Args[0];
                    file = 1;
                }
                else if (e.Args[0].EndsWith(".mp3") || e.Args[0].EndsWith(".flac") || e.Args[0].EndsWith(".wav") || e.Args[0].EndsWith(".aac") || e.Args[0].EndsWith(".m4a"))
                {
                    path = e.Args[0];
                    file = 2;
                }
            }
            new MainWindow(theme,vol,1, path, file, exist).Show();
            SingleInstance<App>.Cleanup();
        }

        bool ISingleInstanceApp.SignalExternalCommandLineArgs(IList<string> args)
        {
            return true;
        }
    }
}
