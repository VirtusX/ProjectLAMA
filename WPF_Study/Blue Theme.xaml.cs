using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;
using Microsoft.Win32;
using TagLib;

namespace WPF_Study
{
    public partial class Blue_Theme : Window
    {
        #region Variables
        private bool userIsDraggingSlider = false;
        public MediaPlayer mediaPlayer = new MediaPlayer();
        bool open = false;
        double LastPos = 0;
        public int LastTrack = 0;
        public List<string> files = new List<string>();
        public List<string> paths = new List<string>();
        public int NowPlay;
        public int ThemeChange = 0;
        double VolChange;
        bool VolChanged = false;
        bool RepeatOn = false;
        int FileExt;
        string musicLoaded;
        #endregion

        public Blue_Theme(double vol = 1, int theme = 0, string MusicLoad = null, int fileExt = 0, int PlaylistExist = 0)
        {
            InitializeComponent();
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;
            timer.Start();
            FileExt = fileExt;
            musicLoaded = MusicLoad;
            if (musicLoaded != null)
                MusicLoading();
            if (PlaylistExist != 0)
                PlaylistLoad();
            ThemeChange = theme;
            mediaPlayer.Volume = vol;
            Vol.Value = vol;
            VolChange = vol;
            if (Playlist.Items.Count > 0)
            {
                Playlist.SelectedIndex = NowPlay;
            }
            else
                Playlist.SelectedIndex = 0;
        }

        #region Window Control
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Theme = 1;
            if (paths.Count > 0)
            {
                Properties.Settings.Default.Track = new List<string>();
                Properties.Settings.Default.Track.AddRange(paths);
            }
            Properties.Settings.Default.Volume = mediaPlayer.Volume;
            Properties.Settings.Default.NowPlay = NowPlay;
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
                VolChanged = true;
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
                VolChanged = false;
            }
            else
            {
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

        void MusicLoading()
        {
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            if (FileExt == 1)
            {
                var PlaylistLoaded = new List<string>();
                PlaylistLoaded.AddRange(System.IO.File.ReadAllLines(musicLoaded));
                int Count = int.Parse(PlaylistLoaded[0]);
                for (int i = 1; i < Count + 1; i++)
                {
                    if (System.IO.File.Exists(PlaylistLoaded[i]))
                    {
                        paths.Add(PlaylistLoaded[i]);
                        files.Add(PlaylistLoaded[i + Count]);
                    }
                    else
                    {
                        PlaylistLoaded.RemoveAt(i);
                        i--;
                        PlaylistLoaded.RemoveAt(i + Count);
                        Count--;
                    }
                }
                mediaPlayer.Open(new Uri(paths[0]));
                for (int i = LastTrack; i < files.Count; i++)
                {
                    Playlist.Items.Add(files[i]);
                    if (i == LastTrack)
                    {
                        CoverLoad(i);
                        TopTitle.Text = files[i];
                        BlueWindow.Title = files[i];
                    }
                }
                if (Playlist.Items.Count > 0)
                {
                    Playlist.SelectedIndex = LastTrack;
                    mediaPlayer.Play();
                }
                NowPlay = LastTrack;
                LastTrack = files.Count;
                open = false;
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
            else if (FileExt == 2)
            {
                mediaPlayer.Open(new Uri(musicLoaded));
                files.Add(Path.GetFileName(musicLoaded));
                paths.Add(musicLoaded);
                for (int i = LastTrack; i < files.Count; i++)
                {
                    Playlist.Items.Add(files[i]);
                    if (i == LastTrack)
                    {
                        CoverLoad(i);
                        TopTitle.Text = files[i];
                        BlueWindow.Title = files[i];
                    }
                }
                if (Playlist.Items.Count > 0)
                {
                    Playlist.SelectedIndex = LastTrack;
                    mediaPlayer.Play();
                }
                NowPlay = LastTrack;
                LastTrack = files.Count;
                open = false;
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
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Paused;
            }
        }

        private void PreviousCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (files.Count > 1)
            {
                RepeatButton.IsChecked = false;
                RepeatOn = false;
                if (NowPlay != 0)
                {
                    NowPlay--;
                    Playlist.SelectedIndex = NowPlay;
                }
                else
                {
                    NowPlay = files.Count - 1;
                    Playlist.SelectedIndex = NowPlay;
                }
                mediaPlayer.Open(new Uri(paths[NowPlay]));
                mediaPlayer.Play();
                TopTitle.Text = files[NowPlay];
                BlueWindow.Title = files[NowPlay];
                CoverLoad(NowPlay);
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
                RepeatOn = false;
                if (NowPlay != files.Count - 1)
                {
                    NowPlay++;
                    Playlist.SelectedIndex = NowPlay;
                }
                else
                {
                    NowPlay = 0;
                    Playlist.SelectedIndex = NowPlay;
                }
                mediaPlayer.Open(new Uri(paths[NowPlay]));
                mediaPlayer.Play();
                TopTitle.Text = files[NowPlay];
                BlueWindow.Title = files[NowPlay];
                CoverLoad(NowPlay);
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
                RepeatOn = false;
                if (NowPlay != files.Count - 1)
                {
                    NowPlay++;
                    Playlist.SelectedIndex = NowPlay;
                }
                else
                {
                    NowPlay = 0;
                    Playlist.SelectedIndex = NowPlay;
                }
                mediaPlayer.Open(new Uri(paths[NowPlay]));
                mediaPlayer.Play();
                TopTitle.Text = files[NowPlay];
                BlueWindow.Title = files[NowPlay];
                CoverLoad(NowPlay);
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
                RepeatOn = false;
                if (NowPlay != 0)
                {
                    NowPlay--;
                    Playlist.SelectedIndex = NowPlay;
                }
                else
                {
                    NowPlay = files.Count - 1;
                    Playlist.SelectedIndex = NowPlay;
                }
                mediaPlayer.Open(new Uri(paths[NowPlay]));
                mediaPlayer.Play();
                TopTitle.Text = files[NowPlay];
                BlueWindow.Title = files[NowPlay];
                CoverLoad(NowPlay);
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
                VolChanged = true;
                LastPos = Vol.Value;
                Vol.Value = 0;
                VolChanged = false;
            }
            if (MuteButton.IsChecked == false)
            {
                VolChanged = true;
                if (LastPos != 0)
                    Vol.Value = LastPos;
                else
                    Vol.Value = 0.05;
                VolChanged = false;
            }
        }

        private void Playlist_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Playlist.Items.Count > 1)
            {
                RepeatButton.IsChecked = false;
                RepeatOn = false;
                if (!open)
                {
                    mediaPlayer.Open(new Uri(paths[Playlist.SelectedIndex]));
                    mediaPlayer.Play();
                    TopTitle.Text = files[Playlist.SelectedIndex];
                    BlueWindow.Title = files[Playlist.SelectedIndex];
                    NowPlay = Playlist.SelectedIndex;
                    CoverLoad(NowPlay);
                }
            }
            PauseButton.IsEnabled = true;
            PlayButton.IsEnabled = false;
            PauseThumb.IsEnabled = true;
            PlayThumb.IsEnabled = false;
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
        }

        private void RepeatButton_Checked(object sender, RoutedEventArgs e)
        {
            if (files.Count > 0)
            {
                RepeatOn = true;
            }
        }

        private void RepeatButton_Unchecked(object sender, RoutedEventArgs e)
        {
            RepeatOn = false;
        }

        private void DeletePlaylist_Click(object sender, RoutedEventArgs e)
        {
            TopTitle.Text = "Project L.A.M.A.";
            BlueWindow.Title = "Project L.A.M.A.";
            Time.Content = "        --:--";
            Visualizer.Source = new BitmapImage(new Uri(@"\Assets\LamaBlue.png", UriKind.Relative));
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
            mediaPlayer.Stop();
            Playlist.Items.Clear();
            files.Clear();
            paths.Clear();
            LastTrack = 0;
            NowPlay = 0;
        }

        private void DeleteTrack_Click(object sender, RoutedEventArgs e)
        {
            if (files.Count > 1)
            {
                if (Playlist.SelectedIndex >= 0)
                {
                    var NewTrack = false;
                    if (Playlist.SelectedIndex == NowPlay)
                        NewTrack = true;
                    files.RemoveAt(Playlist.SelectedIndex);
                    paths.RemoveAt(Playlist.SelectedIndex);
                    Playlist.Items.RemoveAt(Playlist.SelectedIndex);
                    LastTrack--;
                    RepeatButton.IsChecked = false;
                    RepeatOn = false;
                    if (NewTrack)
                    {
                        mediaPlayer.Open(new Uri(paths[NowPlay]));
                        mediaPlayer.Play();
                        TopTitle.Text = files[NowPlay];
                        BlueWindow.Title = files[NowPlay];
                        CoverLoad(NowPlay);
                    }
                }
            }
        }

        void CoverLoad(int i)
        {
            TagLib.File file = TagLib.File.Create((paths[i]));
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
                    Visualizer.Source = new BitmapImage(new Uri(filteredFiles[0]));
                else
                    Visualizer.Source = new BitmapImage(new Uri(@"\Assets\LamaBlue.png", UriKind.Relative));

            }
        }

        void PlaylistLoad()
        {
            foreach (var name in Properties.Settings.Default.Track)
            {
                if (System.IO.File.Exists(Path.GetFileName(name)))
                {
                    files.Add(Path.GetFileName(name));
                    paths.Add(name);
                    Playlist.Items.Add(Path.GetFileName(name));
                }
            }
            if (paths.Count > 0)
            {
                mediaPlayer.Open(new Uri(paths[Properties.Settings.Default.NowPlay]));
                CoverLoad(Properties.Settings.Default.NowPlay);
                TopTitle.Text = files[Properties.Settings.Default.NowPlay];
                BlueWindow.Title = files[Properties.Settings.Default.NowPlay];
                if (Playlist.Items.Count > 0)
                {
                    Playlist.SelectedIndex = Properties.Settings.Default.NowPlay;
                    mediaPlayer.Position = Properties.Settings.Default.Time;
                    mediaPlayer.Pause();
                }
                NowPlay = Properties.Settings.Default.NowPlay;
                LastTrack = files.Count;
                open = false;
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
        }

        #endregion

        #region Sliders
        void timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (mediaPlayer.Source != null)
                    Time.Content = string.Format("{0} / {1}", mediaPlayer.Position.ToString(@"mm\:ss"), mediaPlayer.NaturalDuration.TimeSpan.ToString(@"mm\:ss"));
                else
                {
                    Time.Content = "        --:--";
                    TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                }
                    if ((mediaPlayer.Source != null) && (mediaPlayer.NaturalDuration.HasTimeSpan) && (!userIsDraggingSlider))
                {
                    TrackTime.Minimum = 0;
                    TrackTime.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                    TrackTime.Value = mediaPlayer.Position.TotalSeconds;                 
                    TrackTime.Minimum = 0;
                    TrackTime.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                    TrackTime.Value = mediaPlayer.Position.TotalSeconds;
                    TaskbarItemInfo.ProgressValue = TrackTime.Value / TrackTime.Maximum;
                    if (TrackTime.Value == mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds)
                    {
                        if (files.Count > 1)
                        {
                            if (RepeatOn == false)
                            {
                                if (NowPlay != files.Count - 1)
                                {
                                    NowPlay++;
                                    Playlist.SelectedIndex = NowPlay;
                                }
                                else
                                {
                                    NowPlay = 0;
                                    Playlist.SelectedIndex = NowPlay;
                                }
                                mediaPlayer.Open(new Uri(paths[NowPlay]));
                                mediaPlayer.Play();
                                TopTitle.Text = files[NowPlay];
                                BlueWindow.Title = files[NowPlay];
                                CoverLoad(NowPlay);
                            }
                            else
                                mediaPlayer.Open(new Uri(paths[NowPlay]));
                        }
                        PauseButton.IsEnabled = true;
                        PlayButton.IsEnabled = false;
                        PauseThumb.IsEnabled = true;
                        PlayThumb.IsEnabled = false;
                    }
                }
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
            VolChanged = true;
        }

        private void Vol_DragCompleted(object sender, RoutedEventArgs e)
        {
            VolChanged = false;
        }

        private void Vol_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (ThemeChange == 0 && VolChanged)
                    mediaPlayer.Volume = Vol.Value;
                else
                {
                    ThemeChange = 0;
                    Vol.Value = VolChange;
                    mediaPlayer.Volume = VolChange;
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
            HamburgerButton.IsChecked = false;
            Menu.Visibility = Visibility.Hidden;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            open = true;
            RepeatButton.IsChecked = false;
            RepeatOn = false;
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            openFileDialog.Filter = "All supported files (*.mp3;*.wav;*.aac;*.flac;*.m4a)|*.mp3;*.wav;*.aac;*.flac;*.m4a|MP3 files (*.mp3)|*.mp3|WAV files (*.wav)|*.wav|AAC files (*.aac)|*.aac|Lossless files (*.flac;*.m4a)|*.flac;*.m4a";
            if (openFileDialog.ShowDialog() == true)
                mediaPlayer.Open(new Uri(openFileDialog.FileName));
            files.AddRange(openFileDialog.SafeFileNames);
            paths.AddRange(openFileDialog.FileNames);
            for (int i = LastTrack; i < files.Count; i++)
            {
                Playlist.Items.Add(files[i]);
                if (i == LastTrack)
                {
                    CoverLoad(i);
                    TopTitle.Text = files[i];
                    BlueWindow.Title = files[i];
                }
            }
            if (Playlist.Items.Count > 0)
            {
                Playlist.SelectedIndex = LastTrack;
                mediaPlayer.Play();
            }
            NowPlay = LastTrack;
            LastTrack = files.Count;
            open = false;
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

        private void OpenPlaylist_Selected(object sender, RoutedEventArgs e)
        {
            try
            {
                HamburgerButton.IsChecked = false;
                Menu.Visibility = Visibility.Hidden;
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "LAMA playlist|*.lpl";
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                if (openFileDialog.ShowDialog() == true)
                {
                    var PlaylistLoaded = new List<string>();
                    PlaylistLoaded.AddRange(System.IO.File.ReadAllLines(openFileDialog.FileName));
                    int Count = int.Parse(PlaylistLoaded[0]);
                    files.Clear();
                    paths.Clear();
                    RepeatButton.IsChecked = false;
                    RepeatOn = false;
                    Playlist.Items.Clear();
                    LastTrack = 0;
                    for (int i = 1; i < Count + 1; i++)
                    {
                        if (System.IO.File.Exists(PlaylistLoaded[i]))
                        {
                            paths.Add(PlaylistLoaded[i]);
                            files.Add(PlaylistLoaded[i + Count]);
                        }
                        else
                        {
                            PlaylistLoaded.RemoveAt(i);
                            i--;
                            PlaylistLoaded.RemoveAt(i + Count);
                            Count--;
                        }
                    }
                    mediaPlayer.Open(new Uri(paths[0]));
                    for (int i = LastTrack; i < files.Count; i++)
                    {
                        Playlist.Items.Add(files[i]);
                        if (i == LastTrack)
                        {
                            CoverLoad(i);
                            TopTitle.Text = files[i];
                            BlueWindow.Title = files[i];
                        }
                    }
                    if (Playlist.Items.Count > 0)
                    {
                        Playlist.SelectedIndex = LastTrack;
                        mediaPlayer.Play();
                    }
                    NowPlay = LastTrack;
                    LastTrack = files.Count;
                    open = false;
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
                saveFileDialog.Filter = "LAMA playlist|*.lpl";
                saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                if (saveFileDialog.ShowDialog() == true)
                {
                    var SaveFile = new List<string>();
                    SaveFile.Add(paths.Count.ToString());
                    SaveFile.AddRange(paths);
                    SaveFile.AddRange(files);
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
            Properties.Settings.Default.NowPlay = NowPlay;
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

        private void RoseThemeButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow RoseWindow = new MainWindow(mediaPlayer.Volume);
            if (files.Count > 0)
            {
                for (int i = 0; i < Playlist.Items.Count; i++)
                {
                    RoseWindow.Playlist.Items.Add(Playlist.Items[i]);
                    RoseWindow.files.Add(files[i]);
                    RoseWindow.paths.Add(paths[i]);
                }
                RoseWindow.NowPlay = NowPlay;
                RoseWindow.LastTrack = LastTrack;
                if (Playlist.Items.Count > 0)
                {
                    RoseWindow.mediaPlayer.Open(new Uri(paths[NowPlay]));
                    RoseWindow.mediaPlayer.Play();
                    RoseWindow.mediaPlayer.Position = mediaPlayer.Position;
                    RoseWindow.Playlist.SelectedIndex = NowPlay;
                    RoseWindow.TopTitle.Text = files[NowPlay];
                    RoseWindow.RoseWindow.Title = files[NowPlay];
                    RoseWindow.RepeatButton.IsChecked = RepeatButton.IsChecked;
                }
                if (PauseButton.IsEnabled == false)
                {
                    RoseWindow.mediaPlayer.Pause();
                    RoseWindow.PauseButton.IsEnabled = false;
                    RoseWindow.PlayButton.IsEnabled = true;
                    RoseWindow.PlayThumb.IsEnabled = true;
                    RoseWindow.PauseThumb.IsEnabled = false;
                }
                else
                {
                    RoseWindow.PauseButton.IsEnabled = true;
                    RoseWindow.PlayButton.IsEnabled = false;
                    RoseWindow.PlayThumb.IsEnabled = false;
                    RoseWindow.PauseThumb.IsEnabled = true;
                }
                if (files.Count > 1)
                {
                    RoseWindow.NextButton.IsEnabled = true;
                    RoseWindow.PreviousButton.IsEnabled = true;
                    RoseWindow.NextThumb.IsEnabled = true;
                    RoseWindow.BackThumb.IsEnabled = true;
                }
                else
                {
                    RoseWindow.NextButton.IsEnabled = false;
                    RoseWindow.PreviousButton.IsEnabled = false;
                    RoseWindow.NextThumb.IsEnabled = false;
                    RoseWindow.BackThumb.IsEnabled = false;
                }
                mediaPlayer.Stop();
                TagLib.File file = TagLib.File.Create((paths[NowPlay]));
                if (file.Tag.Pictures != null && file.Tag.Pictures.Length != 0)
                {
                    IPicture pic = file.Tag.Pictures[0];
                    MemoryStream stream = new MemoryStream(pic.Data.Data);
                    BitmapFrame bmp = BitmapFrame.Create(stream);
                    RoseWindow.Visualizer.Source = bmp;
                }
                else
                {
                    string directoryPath = Path.GetDirectoryName(paths[NowPlay]);
                    var filteredFiles = Directory
                        .EnumerateFiles(directoryPath) //<--- .NET 4.5
                        .Where(files => files.ToLower().EndsWith("png") || files.ToLower().EndsWith("jpg"))
                        .ToList();
                    if (filteredFiles.Count > 0)
                        RoseWindow.Visualizer.Source = new BitmapImage(new Uri(filteredFiles[0]));
                    else
                        RoseWindow.Visualizer.Source = new BitmapImage(new Uri(@"\Assets\LamaRose.png", UriKind.Relative));
                }
            }
            Close();
            RoseWindow.TaskbarItemInfo.ProgressState = TaskbarItemInfo.ProgressState;
            RoseWindow.ThemeChange = 1;
            RoseWindow.Left = Left;
            RoseWindow.Top = Top;
            RoseWindow.Show();
        }

        private void GreyThemeButton_Click(object sender, RoutedEventArgs e)
        {
            Grey_Theme GreyWindow = new Grey_Theme(mediaPlayer.Volume);
            if (files.Count > 0)
            {
                for (int i = 0; i < Playlist.Items.Count; i++)
                {
                    GreyWindow.Playlist.Items.Add(Playlist.Items[i]);
                    GreyWindow.files.Add(files[i]);
                    GreyWindow.paths.Add(paths[i]);
                }
                GreyWindow.NowPlay = NowPlay;
                GreyWindow.LastTrack = LastTrack;
                if (Playlist.Items.Count > 0)
                {
                    GreyWindow.mediaPlayer.Open(new Uri(paths[NowPlay]));
                    GreyWindow.mediaPlayer.Play();
                    GreyWindow.mediaPlayer.Position = mediaPlayer.Position;
                    GreyWindow.Playlist.SelectedIndex = NowPlay;
                    GreyWindow.TopTitle.Text = files[NowPlay];
                    GreyWindow.GreyWindow.Title = files[NowPlay];
                    GreyWindow.RepeatButton.IsChecked = RepeatButton.IsChecked;
                }
                if (PauseButton.IsEnabled == false)
                {
                    GreyWindow.mediaPlayer.Pause();
                    GreyWindow.PauseButton.IsEnabled = false;
                    GreyWindow.PlayButton.IsEnabled = true;
                    GreyWindow.PlayThumb.IsEnabled = true;
                    GreyWindow.PauseThumb.IsEnabled = false;
                }
                else
                {
                    GreyWindow.PauseButton.IsEnabled = true;
                    GreyWindow.PlayButton.IsEnabled = false;
                    GreyWindow.PlayThumb.IsEnabled = false;
                    GreyWindow.PauseThumb.IsEnabled = true;
                }
                if (files.Count > 1)
                {
                    GreyWindow.NextButton.IsEnabled = true;
                    GreyWindow.PreviousButton.IsEnabled = true;
                    GreyWindow.NextThumb.IsEnabled = true;
                    GreyWindow.BackThumb.IsEnabled = true;
                }
                else
                {
                    GreyWindow.NextButton.IsEnabled = false;
                    GreyWindow.PreviousButton.IsEnabled = false;
                    GreyWindow.NextThumb.IsEnabled = false;
                    GreyWindow.BackThumb.IsEnabled = false;
                }
                mediaPlayer.Stop();
                TagLib.File file = TagLib.File.Create((paths[NowPlay]));
                if (file.Tag.Pictures != null && file.Tag.Pictures.Length != 0)
                {
                    IPicture pic = file.Tag.Pictures[0];
                    MemoryStream stream = new MemoryStream(pic.Data.Data);
                    BitmapFrame bmp = BitmapFrame.Create(stream);
                    GreyWindow.Visualizer.Source = bmp;
                }
                else
                {
                    string directoryPath = Path.GetDirectoryName(paths[NowPlay]);
                    var filteredFiles = Directory
                        .EnumerateFiles(directoryPath) //<--- .NET 4.5
                        .Where(files => files.ToLower().EndsWith("png") || files.ToLower().EndsWith("jpg"))
                        .ToList();
                    if (filteredFiles.Count > 0)
                        GreyWindow.Visualizer.Source = new BitmapImage(new Uri(filteredFiles[0]));
                    else
                        GreyWindow.Visualizer.Source = new BitmapImage(new Uri(@"\Assets\LamaGrey.png", UriKind.Relative));
                }
            }
            Close();
            GreyWindow.TaskbarItemInfo.ProgressState = TaskbarItemInfo.ProgressState;
            GreyWindow.ThemeChange = 1;
            GreyWindow.Left = Left;
            GreyWindow.Top = Top;
            GreyWindow.Show();
        }

        private void RedThemeButton_Click(object sender, RoutedEventArgs e)
        {
            Crimson_Theme RedWindow = new Crimson_Theme(mediaPlayer.Volume);
            if (files.Count > 0)
            {
                for (int i = 0; i < Playlist.Items.Count; i++)
                {
                    RedWindow.Playlist.Items.Add(Playlist.Items[i]);
                    RedWindow.files.Add(files[i]);
                    RedWindow.paths.Add(paths[i]);
                }
                RedWindow.NowPlay = NowPlay;
                RedWindow.LastTrack = LastTrack;
                if (Playlist.Items.Count > 0)
                {
                    RedWindow.mediaPlayer.Open(new Uri(paths[NowPlay]));
                    RedWindow.mediaPlayer.Play();
                    RedWindow.mediaPlayer.Position = mediaPlayer.Position;
                    RedWindow.Playlist.SelectedIndex = NowPlay;
                    RedWindow.TopTitle.Text = files[NowPlay];
                    RedWindow.RedWindow.Title = files[NowPlay];
                    RedWindow.RepeatButton.IsChecked = RepeatButton.IsChecked;
                }
                if (PauseButton.IsEnabled == false)
                {
                    RedWindow.mediaPlayer.Pause();
                    RedWindow.PauseButton.IsEnabled = false;
                    RedWindow.PlayButton.IsEnabled = true;
                    RedWindow.PlayThumb.IsEnabled = true;
                    RedWindow.PauseThumb.IsEnabled = false;
                }
                else
                {
                    RedWindow.PauseButton.IsEnabled = true;
                    RedWindow.PlayButton.IsEnabled = false;
                    RedWindow.PlayThumb.IsEnabled = false;
                    RedWindow.PauseThumb.IsEnabled = true;
                }
                if (files.Count > 1)
                {
                    RedWindow.NextButton.IsEnabled = true;
                    RedWindow.PreviousButton.IsEnabled = true;
                    RedWindow.NextThumb.IsEnabled = true;
                    RedWindow.BackThumb.IsEnabled = true;
                }
                else
                {
                    RedWindow.NextButton.IsEnabled = false;
                    RedWindow.PreviousButton.IsEnabled = false;
                    RedWindow.NextThumb.IsEnabled = false;
                    RedWindow.BackThumb.IsEnabled = false;
                }
                RedWindow.Vol.Value = Vol.Value;
                RedWindow.mediaPlayer.Volume = Vol.Value;
                mediaPlayer.Stop();
                TagLib.File file = TagLib.File.Create((paths[NowPlay]));
                if (file.Tag.Pictures != null && file.Tag.Pictures.Length != 0)
                {
                    IPicture pic = file.Tag.Pictures[0];
                    MemoryStream stream = new MemoryStream(pic.Data.Data);
                    BitmapFrame bmp = BitmapFrame.Create(stream);
                    RedWindow.Visualizer.Source = bmp;
                }
                else
                {
                    string directoryPath = Path.GetDirectoryName(paths[NowPlay]);
                    var filteredFiles = Directory
                        .EnumerateFiles(directoryPath) //<--- .NET 4.5
                        .Where(files => files.ToLower().EndsWith("png") || files.ToLower().EndsWith("jpg"))
                        .ToList();
                    if (filteredFiles.Count > 0)
                        RedWindow.Visualizer.Source = new BitmapImage(new Uri(filteredFiles[0]));
                    else
                        RedWindow.Visualizer.Source = new BitmapImage(new Uri(@"\Assets\LamaRed.png", UriKind.Relative));
                }
            }
            Close();
            RedWindow.TaskbarItemInfo.ProgressState = TaskbarItemInfo.ProgressState;
            RedWindow.ThemeChange = 1;
            RedWindow.Left = Left;
            RedWindow.Top = Top;
            RedWindow.Show();
        }

        private void GreenThemeButton_Click(object sender, RoutedEventArgs e)
        {
            Green_Theme GreenWindow = new Green_Theme(mediaPlayer.Volume);
            if (files.Count > 0)
            {
                for (int i = 0; i < Playlist.Items.Count; i++)
                {
                    GreenWindow.Playlist.Items.Add(Playlist.Items[i]);
                    GreenWindow.files.Add(files[i]);
                    GreenWindow.paths.Add(paths[i]);
                }
                GreenWindow.NowPlay = NowPlay;
                GreenWindow.LastTrack = LastTrack;
                if (Playlist.Items.Count > 0)
                {
                    GreenWindow.mediaPlayer.Open(new Uri(paths[NowPlay]));
                    GreenWindow.mediaPlayer.Play();
                    GreenWindow.mediaPlayer.Position = mediaPlayer.Position;
                    GreenWindow.Playlist.SelectedIndex = NowPlay;
                    GreenWindow.TopTitle.Text = files[NowPlay];
                    GreenWindow.GreenWindow.Title = files[NowPlay];
                    GreenWindow.RepeatButton.IsChecked = RepeatButton.IsChecked;
                }
                if (PauseButton.IsEnabled == false)
                {
                    GreenWindow.mediaPlayer.Pause();
                    GreenWindow.PauseButton.IsEnabled = false;
                    GreenWindow.PlayButton.IsEnabled = true;
                    GreenWindow.PlayThumb.IsEnabled = true;
                    GreenWindow.PauseThumb.IsEnabled = false;
                }
                else
                {
                    GreenWindow.PauseButton.IsEnabled = true;
                    GreenWindow.PlayButton.IsEnabled = false;
                    GreenWindow.PlayThumb.IsEnabled = false;
                    GreenWindow.PauseThumb.IsEnabled = true;
                }
                if (files.Count > 1)
                {
                    GreenWindow.NextButton.IsEnabled = true;
                    GreenWindow.PreviousButton.IsEnabled = true;
                    GreenWindow.NextThumb.IsEnabled = true;
                    GreenWindow.BackThumb.IsEnabled = true;
                }
                else
                {
                    GreenWindow.NextButton.IsEnabled = false;
                    GreenWindow.PreviousButton.IsEnabled = false;
                    GreenWindow.NextThumb.IsEnabled = false;
                    GreenWindow.BackThumb.IsEnabled = false;
                }
                mediaPlayer.Stop();
                TagLib.File file = TagLib.File.Create((paths[NowPlay]));
                if (file.Tag.Pictures != null && file.Tag.Pictures.Length != 0)
                {
                    IPicture pic = file.Tag.Pictures[0];
                    MemoryStream stream = new MemoryStream(pic.Data.Data);
                    BitmapFrame bmp = BitmapFrame.Create(stream);
                    GreenWindow.Visualizer.Source = bmp;
                }
                else
                {
                    string directoryPath = Path.GetDirectoryName(paths[NowPlay]);
                    var filteredFiles = Directory
                        .EnumerateFiles(directoryPath) //<--- .NET 4.5
                        .Where(files => files.ToLower().EndsWith("png") || files.ToLower().EndsWith("jpg"))
                        .ToList();
                    if (filteredFiles.Count > 0)
                        GreenWindow.Visualizer.Source = new BitmapImage(new Uri(filteredFiles[0]));
                    else
                        GreenWindow.Visualizer.Source = new BitmapImage(new Uri(@"\Assets\LamaGreen.png", UriKind.Relative));
                }
            }
            Close();
            GreenWindow.TaskbarItemInfo.ProgressState = TaskbarItemInfo.ProgressState;
            GreenWindow.ThemeChange = 1;
            GreenWindow.Left = Left;
            GreenWindow.Top = Top;
            GreenWindow.Show();
        }
        #endregion

    }
}
