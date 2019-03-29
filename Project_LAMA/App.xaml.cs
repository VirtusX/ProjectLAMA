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
            var property = new PlayerProperty{Start = true};  
            try
            {
                if (Settings.Default != null)
                {
                    property.Volume = Settings.Default.Volume;
                    property.Theme = Settings.Default.Theme;
                    if (Settings.Default.Track.Capacity != 0)
                        property.HasPlaylist = true;
                }
            }
            catch (Exception) { }
            if (e.Args.Length == 1)
            {
                if (e.Args[0].EndsWith(".lpl"))
                {
                    property.MusicLoaded = e.Args[0];
                    property.FileExtension = PlayerProperty.Extension.PLAYLIST;
                }
                else if (e.Args[0].EndsWith(".mp3") || e.Args[0].EndsWith(".flac") || e.Args[0].EndsWith(".wav") || e.Args[0].EndsWith(".aac") || e.Args[0].EndsWith(".m4a"))
                {
                    property.MusicLoaded = e.Args[0];
                    property.FileExtension = PlayerProperty.Extension.TRACK;
                }
            }
            new MainWindow(property).Show();
            SingleInstance<App>.Cleanup();
        }

        bool ISingleInstanceApp.SignalExternalCommandLineArgs(IList<string> args)
        {
            return true;
        }
    }
}
