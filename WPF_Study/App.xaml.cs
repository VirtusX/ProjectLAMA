using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Shell;

namespace WPF_Study
{
    public partial class App : Application , ISingleInstanceApp
    {
        private const string Unique = "My_Unique_Application_String";

        private void Application_Startup(object sender, StartupEventArgs e)
        {
                if (SingleInstance<App>.InitializeAsFirstInstance(Unique))
                {
                    int file = 0;
                    string path = null;
                    bool exist = false;
                    int theme = 0;
                    double vol = 1;
                    try
                    {
                        if (WPF_Study.Properties.Settings.Default != null)
                        {
                            vol = WPF_Study.Properties.Settings.Default.Volume;
                            theme = WPF_Study.Properties.Settings.Default.Theme;
                            if (WPF_Study.Properties.Settings.Default.Track.Capacity != 0)
                                exist = true;
                        }
                    }
                    catch(Exception){ }
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

                    new MainWindow(theme, musicLoad: path, fileExt: file, start: 1, vol: vol, hasPlaylist: exist).Show();
                    SingleInstance<App>.Cleanup();
                }
            }
          
        bool ISingleInstanceApp.SignalExternalCommandLineArgs(IList<string> args)
        {
            return true;
        }
    }
}
