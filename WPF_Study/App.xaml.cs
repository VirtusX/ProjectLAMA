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
                double Vol = WPF_Study.Properties.Settings.Default.Volume;
                int PlaylistExist = 0;
                if (WPF_Study.Properties.Settings.Default.Track !=null)
                {
                    PlaylistExist = 1;
                    System.IO.File.AppendAllText("crashlog.txt", WPF_Study.Properties.Settings.Default.Track[0]);
                }
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
                switch (WPF_Study.Properties.Settings.Default.Theme)
                {
                    case 0:
                        MainWindow rose = new MainWindow(MusicLoad: path, fileExt: file, theme: 1, vol:Vol, PlaylistExist: PlaylistExist);
                        rose.Show();
                        break;
                    case 1:
                        Blue_Theme blue = new Blue_Theme(MusicLoad: path, fileExt: file, theme: 1, vol: Vol, PlaylistExist: PlaylistExist);
                        blue.Show();
                        break;
                    case 2:
                        Crimson_Theme red = new Crimson_Theme(MusicLoad: path, fileExt: file, theme: 1, vol: Vol, PlaylistExist: PlaylistExist);
                        red.Show();
                        break;
                    case 3:
                        Green_Theme green = new Green_Theme(MusicLoad: path, fileExt: file, theme: 1, vol: Vol, PlaylistExist: PlaylistExist);
                        green.Show();
                        break;
                    case 4:
                        Grey_Theme grey = new Grey_Theme(MusicLoad: path, fileExt: file, theme: 1, vol: Vol);
                        grey.Show();
                        break;
                }
                SingleInstance<App>.Cleanup();
            }
        }

        bool ISingleInstanceApp.SignalExternalCommandLineArgs(IList<string> args)
        {
            return true;
        }
    }
}
