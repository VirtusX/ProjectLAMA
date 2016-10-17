using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;
using Gma.System.MouseKeyHook;
using Microsoft.Win32;
using TagLib;

namespace WPF_Study
{
    public partial class MainWindow : Window
    {
        #region Variables
        private bool userIsDraggingSlider = false;
        public MediaPlayer mediaPlayer = new MediaPlayer();
        bool paused = false;
        double lastPos = 0;
        public int lastTrack = 0;
        public List<string> files = new List<string>();
        public List<string> paths = new List<string>();
        public int nowPlay;
        public int startProgram = 0; //I dunno, why I did it, but without this volume is loaded incorrectly
        double volChange; // And this
        bool volChanged = false; // Yes-yes-yes
        bool repeatOn = false;
        int fileExt;
        string musicLoaded;
        Uri uri = new Uri(@"\Assets\LamaRose.png", UriKind.Relative);
        bool hasCover = false;
        int keyTimer = 0;
        Track track;
        int activeWindow = 0;

        [DllImport("user32.dll")]
        static extern bool GetAsyncKeyState(int key);
        [DllImport("user32.dll")]
        static extern int GetForegroundWindow();
        #endregion

        public MainWindow(int theme = 0, double vol = 1, int start = 0, string musicLoad = null, int fileExt = 0, bool hasPlaylist = false)
        {
            InitializeComponent();
            ThemeLoader(theme);
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;
            timer.Start();
            this.fileExt = fileExt;
            musicLoaded = musicLoad;
            if (musicLoaded != null)
                LoadMusic();
            else if (hasPlaylist)
                LoadPlaylist();
            startProgram = start;
            mediaPlayer.Volume = vol;
            Vol.Value = vol;
            volChange = vol;
            if (Playlist.Items.Count > 0)
            {
                Playlist.SelectedIndex = nowPlay;
            }
            else
                Playlist.SelectedIndex = 0;
        }

        #region Window Control
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Track = new List<string>();
            if (paths.Count > 0)
            {
                Properties.Settings.Default.Track.AddRange(paths);
            }
            Properties.Settings.Default.NowPlay = nowPlay;
            Properties.Settings.Default.Volume = mediaPlayer.Volume;
            Properties.Settings.Default.Time = mediaPlayer.Position;
            Properties.Settings.Default.Save();
           Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (ThemesChoose.IsVisible == false)
            {
                volChanged = true;
                Slider vol = sender as Slider;
                vol = Vol;
                if (e.Delta > 0)
                {
                    vol.Value += 0.05;
                }
                else
                {
                    vol.Value -= 0.05;
                }
                e.Handled = true;
                volChanged = false;
            }
            else {
                ScrollViewer scrollviewer = sender as ScrollViewer;
                scrollviewer = ThemeChooser;
                if (e.Delta > 0)
                {
                    scrollviewer.LineUp();
                }
                else
                {
                    scrollviewer.LineDown();
                }
                e.Handled = true;
            }

        }

        private void TopTitle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void RoseWindow_Drop(object sender, DragEventArgs e)
        {
            string[] fileDrop = (string[])e.Data.GetData(DataFormats.FileDrop);
            musicLoaded = fileDrop[0];
            if (fileDrop[0].EndsWith(".mp3") || fileDrop[0].EndsWith(".flac") || fileDrop[0].EndsWith(".wav") || fileDrop[0].EndsWith(".aac") || fileDrop[0].EndsWith(".m4a"))
            {
                fileExt = 2;
                LoadMusic();
            }
            else if (fileDrop[0].EndsWith(".lpl"))
            {
                fileExt = 1;
                LoadMusic();
            }
        }


        #endregion

        #region Player Control
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (files.Count > 0)
            {
                mediaPlayer.Play();
                PauseButton.IsEnabled = true;
                PlayButton.IsEnabled = false;
                PauseThumb.IsEnabled = true;
                PlayThumb.IsEnabled = false;
                paused = false;
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (files.Count > 0)
            {
                mediaPlayer.Pause();
                PlayButton.IsEnabled = true;
                PauseButton.IsEnabled = false;
                PauseThumb.IsEnabled = false;
                PlayThumb.IsEnabled = true;
                paused = true;
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Paused;
            }
        }

        private void StartCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (files.Count > 0)
            {
                mediaPlayer.Play();
                PauseButton.IsEnabled = true;
                PlayButton.IsEnabled = false;
                PauseThumb.IsEnabled = true;
                PlayThumb.IsEnabled = false;
                paused = false;
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            }
        }

        private void PauseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (files.Count > 0)
            {
                mediaPlayer.Pause();
                PlayButton.IsEnabled = true;
                PauseButton.IsEnabled = false;
                PauseThumb.IsEnabled = false;
                PlayThumb.IsEnabled = true;
                paused = true;
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Paused;
            }
        }

        private void PreviousCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (files.Count > 1)
            {
                RepeatButton.IsChecked = false;
                repeatOn = false;
                if (nowPlay != 0)
                {
                    nowPlay--;
                    Playlist.SelectedIndex = nowPlay;
                }
                else
                {
                    nowPlay = files.Count - 1;
                    Playlist.SelectedIndex = nowPlay;
                }
                mediaPlayer.Open(new Uri(paths[nowPlay]));
                mediaPlayer.Play();
                TopTitle.Text = files[nowPlay];
                RoseWindow.Title = files[nowPlay];
                LoadCover(nowPlay);
            }
            PauseButton.IsEnabled = true;
            PlayButton.IsEnabled = false;
            PauseThumb.IsEnabled = true;
            PlayThumb.IsEnabled = false;
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
        }

        private void NextCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (files.Count > 1)
            {
                RepeatButton.IsChecked = false;
                repeatOn = false;
                if (nowPlay != files.Count - 1)
                {
                    nowPlay++;
                    Playlist.SelectedIndex = nowPlay;
                }
                else
                {
                    nowPlay = 0;
                    Playlist.SelectedIndex = nowPlay;
                }
                mediaPlayer.Open(new Uri(paths[nowPlay]));
                mediaPlayer.Play();
                TopTitle.Text = files[nowPlay];
                RoseWindow.Title = files[nowPlay];
                LoadCover(nowPlay);
            }
            PauseButton.IsEnabled = true;
            PlayButton.IsEnabled = false;
            PauseThumb.IsEnabled = true;
            PlayThumb.IsEnabled = false;
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (files.Count > 1)
            {
                RepeatButton.IsChecked = false;
                repeatOn = false;
                if (nowPlay != files.Count - 1)
                {
                    nowPlay++;
                    Playlist.SelectedIndex = nowPlay;
                }
                else
                {
                    nowPlay = 0;
                    Playlist.SelectedIndex = nowPlay;
                }
                mediaPlayer.Open(new Uri(paths[nowPlay]));
                mediaPlayer.Play();
                TopTitle.Text = files[nowPlay];
                RoseWindow.Title = files[nowPlay];
                LoadCover(nowPlay);
            }
            PauseButton.IsEnabled = true;
            PlayButton.IsEnabled = false;
            PauseThumb.IsEnabled = true;
            PlayThumb.IsEnabled = false;
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (files.Count > 1)
            {
                RepeatButton.IsChecked = false;
                repeatOn = false;
                if (nowPlay != 0)
                {
                    nowPlay--;
                    Playlist.SelectedIndex = nowPlay;
                }
                else
                {
                    nowPlay = files.Count - 1;
                    Playlist.SelectedIndex = nowPlay;
                }
                mediaPlayer.Open(new Uri(paths[nowPlay]));
                mediaPlayer.Play();
                TopTitle.Text = files[nowPlay];
                RoseWindow.Title = files[nowPlay];
                LoadCover(nowPlay);
            }
            PauseButton.IsEnabled = true;
            PlayButton.IsEnabled = false;
            PauseThumb.IsEnabled = true;
            PlayThumb.IsEnabled = false;
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
        }

        private void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (MuteButton.IsChecked == true)
            {
                volChanged = true;
                lastPos = Vol.Value;
                Vol.Value = 0;
                volChanged = false;
            }
            if (MuteButton.IsChecked == false)
            {
                volChanged = true;
                if (lastPos != 0)
                    Vol.Value = lastPos;
                else
                    Vol.Value = 0.05;
                volChanged = false;
            }
        }

        private void Playlist_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Playlist.Items.Count > 1)
            {
                RepeatButton.IsChecked = false;
                repeatOn = false;
                    mediaPlayer.Open(new Uri(paths[Playlist.SelectedIndex]));
                    mediaPlayer.Play();
                    TopTitle.Text = files[Playlist.SelectedIndex];
                    RoseWindow.Title = files[Playlist.SelectedIndex];
                    nowPlay = Playlist.SelectedIndex;
                    LoadCover(nowPlay);
            }
            PauseButton.IsEnabled = true;
            PlayButton.IsEnabled = false;
            PauseThumb.IsEnabled = true;
            PlayThumb.IsEnabled = false;
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            if (track != null)
                track.Close();
            track = new Track(TopTitle.Text, this);
            track.Show();
        }

        private void RepeatButton_Checked(object sender, RoutedEventArgs e)
        {
            if (files.Count > 0)
            {
                repeatOn = true;
            }
        }

        private void RepeatButton_Unchecked(object sender, RoutedEventArgs e)
        {
            repeatOn = false;
        }

        private void DeletePlaylist_Click(object sender, RoutedEventArgs e)
        {
            TopTitle.Text = "Project L.A.M.A.";
            RoseWindow.Title = "Project L.A.M.A.";
            Time.Content = "        --:--";
            Visualizer.Source = new BitmapImage(uri);
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
            mediaPlayer.Stop();
            Playlist.Items.Clear();
            files.Clear();
            paths.Clear();
            lastTrack = 0;
            hasCover = false;
            nowPlay = 0;
        }

        private void DeleteTrack_Click(object sender, RoutedEventArgs e)
        {
            if (files.Count > 1)
            {
                if (Playlist.SelectedIndex >= 0)
                {
                    var NewTrack = false;
                    if (Playlist.SelectedIndex == nowPlay)
                        NewTrack = true;
                    files.RemoveAt(Playlist.SelectedIndex);
                    paths.RemoveAt(Playlist.SelectedIndex);
                    Playlist.Items.RemoveAt(Playlist.SelectedIndex);
                    lastTrack--;
                    RepeatButton.IsChecked = false;
                    repeatOn = false;
                    if (NewTrack)
                    {
                        mediaPlayer.Open(new Uri(paths[nowPlay]));
                        mediaPlayer.Play();
                        TopTitle.Text = files[nowPlay];
                        RoseWindow.Title = files[nowPlay];
                        LoadCover(nowPlay);
                    }
                }
            }
        }

        private void RoseWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                MusicControl(1);
            if (e.Key == Key.Down)
            {
                volChanged = true;
                Vol.Value -= 0.02;
                volChanged = false;
            }
            if(e.Key == Key.Up)
            {
                volChanged = true;
                Vol.Value += 0.02;
                volChanged = false;
            }
        }

        private void Playlist_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                MusicControl(1);
        }

        void MusicControl(int i)
        {
            if (i == 1)
            {
                if (files.Count > 0)
                {
                    if (paused)
                    {
                        mediaPlayer.Play();
                        PauseButton.IsEnabled = true;
                        PlayButton.IsEnabled = false;
                        PauseThumb.IsEnabled = true;
                        PlayThumb.IsEnabled = false;
                        paused = false;
                        TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                    }
                    else
                    {
                        mediaPlayer.Pause();
                        PlayButton.IsEnabled = true;
                        PauseButton.IsEnabled = false;
                        PauseThumb.IsEnabled = false;
                        PlayThumb.IsEnabled = true;
                        paused = true;
                        TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Paused;
                    }
                }
            }
            else if (i == 2)
            {
                if (files.Count > 1)
                {
                    RepeatButton.IsChecked = false;
                    repeatOn = false;
                    if (nowPlay != 0)
                    {
                        nowPlay--;
                        Playlist.SelectedIndex = nowPlay;
                    }
                    else
                    {
                        nowPlay = files.Count - 1;
                        Playlist.SelectedIndex = nowPlay;
                    }
                    mediaPlayer.Open(new Uri(paths[nowPlay]));
                    mediaPlayer.Play();
                    TopTitle.Text = files[nowPlay];
                    RoseWindow.Title = files[nowPlay];
                    LoadCover(nowPlay);
                    PauseButton.IsEnabled = true;
                    PlayButton.IsEnabled = false;
                    PauseThumb.IsEnabled = true;
                    PlayThumb.IsEnabled = false;
                    TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                    if (track != null)
                    {
                        track.Close();
                    }
                    track = new Track(TopTitle.Text, this);
                    track.Show();
                }
            }
            else if (i == 3)
            {
                if (files.Count > 1)
                {
                    RepeatButton.IsChecked = false;
                    repeatOn = false;
                    if (nowPlay != files.Count - 1)
                    {
                        nowPlay++;
                        Playlist.SelectedIndex = nowPlay;
                    }
                    else
                    {
                        nowPlay = 0;
                        Playlist.SelectedIndex = nowPlay;
                    }
                    mediaPlayer.Open(new Uri(paths[nowPlay]));
                    mediaPlayer.Play();
                    TopTitle.Text = files[nowPlay];
                    RoseWindow.Title = files[nowPlay];
                    LoadCover(nowPlay);
                    PauseButton.IsEnabled = true;
                    PlayButton.IsEnabled = false;
                    PauseThumb.IsEnabled = true;
                    PlayThumb.IsEnabled = false;
                    TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                     if (track != null)
                        track.Close();
                    track = new Track(TopTitle.Text, this);
                    track.Show();
                }
            }
        }

        void LoadCover(int i)
        {
            try
            {
                TagLib.File file = TagLib.File.Create((paths[i]));
                hasCover = true;
                if (file.Tag.Pictures != null && file.Tag.Pictures.Length != 0)
                {
                    IPicture pic = file.Tag.Pictures[0];
                    MemoryStream stream = new MemoryStream(pic.Data.Data);
                    BitmapFrame bmp = BitmapFrame.Create(stream);
                    Visualizer.Source = bmp;
                }
                else
                {
                    string directoryPath = Path.GetDirectoryName(paths[i]);
                    var filteredFiles = Directory
                        .EnumerateFiles(directoryPath) //<--- .NET 4.5
                        .Where(files => files.ToLower().EndsWith("png") || files.ToLower().EndsWith("jpg"))
                        .ToList();
                    if (filteredFiles.Count > 0)
                    {
                        Visualizer.Source = new BitmapImage(new Uri(filteredFiles[0]));
                    }
                    else
                    {
                        Visualizer.Source = new BitmapImage(uri);
                        hasCover = false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("crashlog.txt", ex.Message.ToString());
            }
        }

        void LoadMusic()
        {
            if (fileExt == 1)
            {
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                var playlistLoaded = new List<string>();
                playlistLoaded.AddRange(System.IO.File.ReadAllLines(musicLoaded));
                int Count = int.Parse(playlistLoaded[0]);
                for (int i = 1; i < Count + 1; i++)
                {
                    if (System.IO.File.Exists(playlistLoaded[i]))
                    {
                        paths.Add(playlistLoaded[i]);
                        files.Add(Path.GetFileNameWithoutExtension(playlistLoaded[i]));
                    }
                    else
                    {
                        playlistLoaded.RemoveAt(i);
                        i--;
                        Count--;
                    }
                }
                mediaPlayer.Open(new Uri(paths[lastTrack]));
                for (int i = lastTrack; i < files.Count; i++)
                {
                    Playlist.Items.Add(files[i]);
                    if (i == lastTrack)
                    {
                        LoadCover(i);
                        TopTitle.Text = files[i];
                        RoseWindow.Title = files[i];
                    }
                }
                if (Playlist.Items.Count > 0)
                {
                    Playlist.SelectedIndex = lastTrack;
                    mediaPlayer.Play();
                }
                nowPlay = lastTrack;
                lastTrack = files.Count;
                if (files.Count > 1)
                {
                    NextButton.IsEnabled = true;
                    PreviousButton.IsEnabled = true;
                    NextThumb.IsEnabled = true;
                    BackThumb.IsEnabled = true;
                }
                else
                {
                    NextButton.IsEnabled = false;
                    PreviousButton.IsEnabled = false;
                    NextThumb.IsEnabled = false;
                    BackThumb.IsEnabled = false;
                }
                PauseButton.IsEnabled = true;
                PlayButton.IsEnabled = false;
                PauseThumb.IsEnabled = true;
                PlayThumb.IsEnabled = false;
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            }
            else if (fileExt == 2)
            {
                mediaPlayer.Open(new Uri(musicLoaded));
                files.Add(Path.GetFileNameWithoutExtension(musicLoaded));
                paths.Add(musicLoaded);
                for (int i = lastTrack; i < files.Count; i++)
                {
                    Playlist.Items.Add(files[i]);
                    if (i == lastTrack)
                    {
                        LoadCover(i);
                        TopTitle.Text = files[i];
                        RoseWindow.Title = files[i];
                    }
                }
                if (Playlist.Items.Count > 0)
                {
                    Playlist.SelectedIndex = lastTrack;
                    mediaPlayer.Play();
                }
                nowPlay = lastTrack;
                lastTrack = files.Count;
                if (files.Count > 1)
                {
                    NextButton.IsEnabled = true;
                    PreviousButton.IsEnabled = true;
                    NextThumb.IsEnabled = true;
                    BackThumb.IsEnabled = true;
                }
                else
                {
                    NextButton.IsEnabled = false;
                    PreviousButton.IsEnabled = false;
                    NextThumb.IsEnabled = false;
                    BackThumb.IsEnabled = false;
                }
                PauseButton.IsEnabled = true;
                PlayButton.IsEnabled = false;
                PauseThumb.IsEnabled = true;
                PlayThumb.IsEnabled = false;
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            }
        }

        void LoadPlaylist()
        {
            foreach (var name in Properties.Settings.Default.Track)
            {
                if (System.IO.File.Exists(new FileInfo(name).FullName))
                {
                    files.Add(Path.GetFileNameWithoutExtension(name));
                    paths.Add(name);
                    Playlist.Items.Add(Path.GetFileNameWithoutExtension(name));
                }
            }
            if (paths.Capacity > 0)
            {
                try
                {
                    mediaPlayer.Open(new Uri(paths[Properties.Settings.Default.NowPlay]));
                    LoadCover(Properties.Settings.Default.NowPlay);
                    TopTitle.Text = files[Properties.Settings.Default.NowPlay];
                    RoseWindow.Title = files[Properties.Settings.Default.NowPlay];
                    if (Playlist.Items.Count > 0)
                    {
                        Playlist.SelectedIndex = Properties.Settings.Default.NowPlay;
                        mediaPlayer.Position = Properties.Settings.Default.Time;
                        mediaPlayer.Pause();
                    }
                    nowPlay = Properties.Settings.Default.NowPlay;
                    lastTrack = files.Count;
                    if (files.Count > 1)
                    {
                        NextButton.IsEnabled = true;
                        PreviousButton.IsEnabled = true;
                        NextThumb.IsEnabled = true;
                        BackThumb.IsEnabled = true;
                    }
                    else
                    {
                        NextButton.IsEnabled = false;
                        PreviousButton.IsEnabled = false;
                        NextThumb.IsEnabled = false;
                        BackThumb.IsEnabled = false;
                    }
                    PauseButton.IsEnabled = false;
                    PlayButton.IsEnabled = true;
                    PauseThumb.IsEnabled = false;
                    PlayThumb.IsEnabled = true;
                    TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Paused;
                    Properties.Settings.Default.Track.Clear();
                    Properties.Settings.Default.NowPlay = 0;
                    Properties.Settings.Default.Time = TimeSpan.Zero;
                }
                catch(Exception e)
                {
                    System.IO.File.AppendAllText("Exception.txt", e.Message + " ");
                }
            }
        }

        #endregion

        #region Sliders
        void timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (paths.Count == 0)
                {
                    NextButton.IsEnabled = false;
                    PauseButton.IsEnabled = false;
                    PlayButton.IsEnabled = false;
                    PreviousButton.IsEnabled = false;
                    RepeatButton.IsEnabled = false;
                }
                else
                    RepeatButton.IsEnabled = true;
                if(activeWindow == 0)
                {
                    activeWindow = GetForegroundWindow();
                }
                int windowState = GetForegroundWindow();
                if (GetAsyncKeyState(0xB3) && keyTimer == 0)
                {
                    MusicControl(1);
                    keyTimer = 1;
                }
                if (GetAsyncKeyState(0xB1) && keyTimer == 0 && windowState != activeWindow)
                {
                    MusicControl(2);
                    keyTimer = 1;
                }
                if (GetAsyncKeyState(0xB0) && keyTimer == 0 && windowState != activeWindow)
                {
                    MusicControl(3);
                    keyTimer = 1;
                }
                if (mediaPlayer.Source != null && paths.Count != 0)
                    Time.Content = string.Format("{0} / {1}", mediaPlayer.Position.ToString(@"mm\:ss"), mediaPlayer.NaturalDuration.TimeSpan.ToString(@"mm\:ss"));
                else if(paths.Count == 0)
                {
                    Time.Content = "        --:--";
                    TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                }
                if ((mediaPlayer.Source != null) && (mediaPlayer.NaturalDuration.HasTimeSpan) && (!userIsDraggingSlider))
                {
                    TrackTime.Minimum = 0;
                    TrackTime.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                    TrackTime.Value = mediaPlayer.Position.TotalSeconds;
                    TaskbarItemInfo.ProgressValue = TrackTime.Value / TrackTime.Maximum;
                    if (TrackTime.Value == mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds)
                    {
                        
                        if (files.Count >= 1)
                        {
                            if (repeatOn == false)
                            {
                                if (nowPlay != files.Count - 1)
                                {
                                    nowPlay++;
                                    Playlist.SelectedIndex = nowPlay;
                                }
                                else
                                {
                                    nowPlay = 0;
                                    Playlist.SelectedIndex = nowPlay;
                                }
                                mediaPlayer.Open(new Uri(paths[nowPlay]));
                                mediaPlayer.Play();
                                TopTitle.Text = files[nowPlay];
                                RoseWindow.Title = files[nowPlay];
                                LoadCover(nowPlay);
                                if (track != null)
                                    track.Close();
                                track = new Track(TopTitle.Text, this);
                                track.Show();
                            }
                            else
                            {
                                mediaPlayer.Open(new Uri(paths[nowPlay]));
                                if (track != null)
                                    track.Close();
                                track = new Track(TopTitle.Text, this);
                                track.Show();
                            }
                            }
                        PauseButton.IsEnabled = true;
                        PlayButton.IsEnabled = false;
                        PauseThumb.IsEnabled = true;
                        PlayThumb.IsEnabled = false;
                    }
                }
                if(keyTimer!=0)
                    keyTimer--;
            }
            catch { }
        }

        private void TrackTime_DragStarted(object sender, DragStartedEventArgs e)
        {
            userIsDraggingSlider = true;
        }

        private void TrackTime_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            userIsDraggingSlider = false;
            mediaPlayer.Position = TimeSpan.FromSeconds(TrackTime.Value);
        }

        private void TrackTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TrackTime.SelectionEnd = TrackTime.Value;
        }

        private void Vol_DragStarted(object sender, RoutedEventArgs e)
        {
            volChanged = true;
        }

        private void Vol_DragCompleted(object sender, RoutedEventArgs e)
        {
            volChanged = false;
        }

        private void Vol_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (startProgram == 0 && volChanged)
                    mediaPlayer.Volume = Vol.Value;
                else
                {
                    startProgram = 0;
                    Vol.Value = volChange;
                    mediaPlayer.Volume = volChange;
                }
                if (Vol.Value == 0)
                {
                    MuteButton.IsChecked = true;
                }

                if (Vol.Value > 0)
                {
                    MuteButton.IsChecked = false;
                }
            }
            catch { };
        }
        #endregion

        #region Menu Control
        private void HamburgerButton_Checked(object sender, RoutedEventArgs e)
        {
            Menu.Visibility = Visibility.Visible;
            OpenFile.IsSelected = false;
            OpenPlaylist.IsSelected = false;
            Save.IsSelected = false;
            Themes.IsSelected = false;
        }

        private void HamburgerButton_Unchecked(object sender, RoutedEventArgs e)
        {
            Menu.Visibility = Visibility.Hidden;
        }

        private void OpenFile_Selected(object sender, RoutedEventArgs e)
        {
            try
            {
                HamburgerButton.IsChecked = false;
                Menu.Visibility = Visibility.Hidden;
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Multiselect = true;
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                openFileDialog.Filter = "All supported files (*.mp3;*.wav;*.aac;*.flac;*.m4a)|*.mp3;*.wav;*.aac;*.flac;*.m4a|MP3 files (*.mp3)|*.mp3|WAV files (*.wav)|*.wav|AAC files (*.aac)|*.aac|Lossless files (*.flac;*.m4a)|*.flac;*.m4a";
                if (openFileDialog.ShowDialog() == true)
                    mediaPlayer.Open(new Uri(openFileDialog.FileName));
                RepeatButton.IsChecked = false;
                repeatOn = false;
                foreach (string file in openFileDialog.FileNames)
                    files.Add(Path.GetFileNameWithoutExtension(file));
                paths.AddRange(openFileDialog.FileNames);
                for (int i = lastTrack; i < files.Count; i++)
                {
                    Playlist.Items.Add(files[i]);
                    if (i == lastTrack)
                    {
                        LoadCover(i);
                        TopTitle.Text = files[i];
                        RoseWindow.Title = files[i];
                    }
                }
                if (Playlist.Items.Count > 0)
                {
                    Playlist.SelectedIndex = lastTrack;
                    mediaPlayer.Play();
                }
                nowPlay = lastTrack;
                lastTrack = files.Count;
                if (files.Count > 1)
                {
                    NextButton.IsEnabled = true;
                    PreviousButton.IsEnabled = true;
                    NextThumb.IsEnabled = true;
                    BackThumb.IsEnabled = true;
                }
                else
                {
                    NextButton.IsEnabled = false;
                    PreviousButton.IsEnabled = false;
                    NextThumb.IsEnabled = false;
                    BackThumb.IsEnabled = false;
                }
                PauseButton.IsEnabled = true;
                PlayButton.IsEnabled = false;
                PauseThumb.IsEnabled = true;
                PlayThumb.IsEnabled = false;
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("crashlog.txt", ex.Message.ToString());
            }
        }

        private void OpenPlaylist_Selected(object sender, RoutedEventArgs e)
        {
            try
            {
                HamburgerButton.IsChecked = false;
                Menu.Visibility = Visibility.Hidden;
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                openFileDialog.Filter = "LAMA playlist|*.lpl";
                if (openFileDialog.ShowDialog() == true)
                {
                    var PlaylistLoaded = new List<string>();
                    PlaylistLoaded.AddRange(System.IO.File.ReadAllLines(openFileDialog.FileName));
                    int Count = int.Parse(PlaylistLoaded[0]);
                    files.Clear();
                    paths.Clear();
                    RepeatButton.IsChecked = false;
                    repeatOn = false;
                    Playlist.Items.Clear();
                    lastTrack = 0;
                    for (int i = 1; i <= Count; i++)
                    {
                        if (System.IO.File.Exists(new FileInfo(PlaylistLoaded[i]).FullName))
                        {
                            files.Add(Path.GetFileNameWithoutExtension(PlaylistLoaded[i]));
                            paths.Add(PlaylistLoaded[i]);
                        }
                        else
                        {
                            PlaylistLoaded.RemoveAt(i);
                            i--;               
                            Count--;
                        }
                    }
                    mediaPlayer.Open(new Uri(paths[0]));
                    for (int i = lastTrack; i < files.Count; i++)
                    {
                        Playlist.Items.Add(files[i]);
                        if (i == lastTrack)
                        {
                            LoadCover(i);
                            TopTitle.Text = files[i];
                            RoseWindow.Title = files[i];
                        }
                    }
                    if (Playlist.Items.Count > 0)
                    {
                        Playlist.SelectedIndex = lastTrack;
                        mediaPlayer.Play();
                    }
                    nowPlay = lastTrack;
                    lastTrack = files.Count;
                    if (files.Count > 1)
                    {
                        NextButton.IsEnabled = true;
                        PreviousButton.IsEnabled = true;
                        NextThumb.IsEnabled = true;
                        BackThumb.IsEnabled = true;
                    }
                    else
                    {
                        NextButton.IsEnabled = false;
                        PreviousButton.IsEnabled = false;
                        NextThumb.IsEnabled = false;
                        BackThumb.IsEnabled = false;
                    }
                    PauseButton.IsEnabled = true;
                    PlayButton.IsEnabled = false;
                    PauseThumb.IsEnabled = true;
                    PlayThumb.IsEnabled = false;
                    TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                }
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("crashlog.txt", ex.Message.ToString());
            }
        }

        private void Save_Selected(object sender, RoutedEventArgs e)
        {
            if (files.Count > 0)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.FileName = "Playlist";
                saveFileDialog.Filter = "LAMA playlist|*.lpl";
                saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                if (saveFileDialog.ShowDialog() == true)
                {
                    var SaveFile = new List<string>();
                    SaveFile.Add(paths.Count.ToString());
                    SaveFile.AddRange(paths);
                    System.IO.File.WriteAllLines(saveFileDialog.FileName.ToString(), SaveFile);
                }
                HamburgerButton.IsChecked = false;
                Menu.Visibility = Visibility.Hidden;
            }
        }

        private void Close_Selected(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Theme = 0;
            if (paths.Count > 0)
            {
                Properties.Settings.Default.Track = new List<string>();
                Properties.Settings.Default.Track.AddRange(paths);
            }
            Properties.Settings.Default.NowPlay = nowPlay;
            Properties.Settings.Default.Volume = mediaPlayer.Volume;
            Properties.Settings.Default.Time = mediaPlayer.Position;
            Properties.Settings.Default.Save();
            Close();
        }

        private void Themes_Selected(object sender, RoutedEventArgs e)
        {
            HamburgerButton.IsChecked = false;
            Menu.Visibility = Visibility.Hidden;
            ThemesChoose.Visibility = Visibility.Visible;
        }
        #endregion

        #region ThemeChoose
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ThemesChoose.Visibility = Visibility.Hidden;
        }

        private void ThemeLoader(int theme)
        {
            switch (theme)
            {
                case 0:
                    uri = new Uri(@"\Assets\LamaRose.png", UriKind.Relative);
                    if (!hasCover)
                        Visualizer.Source = new BitmapImage(uri);
                    Properties.Settings.Default.Theme = 0;
                    Resources = new ResourceDictionary() { Source = new Uri("pack://application:,,,/RoseDictionary.xaml") };
                    break;
                case 1:
                    uri = new Uri(@"\Assets\LamaBlue.png", UriKind.Relative);
                    if (!hasCover)
                        Visualizer.Source = new BitmapImage(uri);
                    Resources = new ResourceDictionary() { Source = new Uri("pack://application:,,,/BlueDictionary.xaml") };
                    Properties.Settings.Default.Theme = 1;
                    break;
                case 2:
                    uri = new Uri(@"\Assets\LamaRed.png", UriKind.Relative);
                    if (!hasCover)
                        Visualizer.Source = new BitmapImage(uri);
                    Properties.Settings.Default.Theme = 2;
                    Resources = new ResourceDictionary() { Source = new Uri("pack://application:,,,/CrimsonDictionary.xaml") };
                    break;
                case 3:
                    uri = new Uri(@"\Assets\LamaGreen.png", UriKind.Relative);
                    if (!hasCover)
                        Visualizer.Source = new BitmapImage(uri);
                    Properties.Settings.Default.Theme = 3;
                    Resources = new ResourceDictionary() { Source = new Uri("pack://application:,,,/GreenDictionary.xaml") };
                    break;
                case 4:
                    uri = new Uri(@"\Assets\LamaGrey.png", UriKind.Relative);
                    if (!hasCover)
                        Visualizer.Source = new BitmapImage(uri);
                    Properties.Settings.Default.Theme = 4;
                    Resources = new ResourceDictionary() { Source = new Uri("pack://application:,,,/GreyDictionary.xaml") };
                    break;
            }
        }

        private void RoseThemeButton_Click(object sender, RoutedEventArgs e)
        {
            ThemeLoader(0);
        }

        private void BlueThemeButton_Click(object sender, RoutedEventArgs e)
        {
            ThemeLoader(1);
        }

        private void RedThemeButton_Click(object sender, RoutedEventArgs e)
        {
            ThemeLoader(2);

        }

        private void GreenThemeButton_Click(object sender, RoutedEventArgs e)
        {
            ThemeLoader(3);

        }

        private void GreyThemeButton_Click(object sender, RoutedEventArgs e)
        {
            ThemeLoader(4);

        }
        #endregion

    }
}
