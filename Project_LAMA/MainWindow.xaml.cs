﻿using System;
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
using Microsoft.Win32;
using static ProjectLama.Properties.Settings;

namespace ProjectLama
{
    partial class MainWindow : Window
    {
        #region Variables
        private bool _userIsDraggingSlider;
        public MediaPlayer MediaPlayer = new MediaPlayer();
        private bool _paused;
        private double _lastPos;
        public int LastTrack;
        public List<string> Files = new List<string>();
        public List<string> Paths = new List<string>();
        public int NowPlay;
        private readonly double _volChange;
        private bool _volChanged;
        private bool _repeatOn;
        private Uri _uri = new Uri(@"\Assets\LamaRose.png", UriKind.Relative);
        private bool _hasCover;
        private int _keyTimer;
        private Track _track;
        private int _activeWindow;
        private PlayerProperty PlayerProperty { get; }
        [DllImport("user32.dll")]
        private static extern bool GetAsyncKeyState(int key);
        [DllImport("user32.dll")]
        private static extern int GetForegroundWindow();
        #endregion

        public MainWindow(PlayerProperty prop = null)
        {
            InitializeComponent();
            PlayerProperty = prop ?? new PlayerProperty();
            Playlist.ItemContainerGenerator.StatusChanged += (sender, args) => SetPlaylistItems();
            SetTheme((PlayerTheme)PlayerProperty.Theme);
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += Timer_Tick;
            timer.Start();
            Visualizer.Source = new BitmapImage(_uri);
            if (PlayerProperty.MusicLoaded != null)
                LoadMusic();
            else if (PlayerProperty.HasPlaylist)
                LoadLastPlaylist();
            Vol.Value = MediaPlayer.Volume = _volChange = PlayerProperty.Volume;
            Playlist.SelectedIndex = Playlist.Items.Count > 0 ? NowPlay : 0;
        }


        #region Window Control
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Default.Track = new List<string>();
            if (Paths.Count > 0) Default.Track.AddRange(Paths);
            Default.NowPlay = NowPlay;
            Default.Volume = MediaPlayer.Volume;
            Default.Time = MediaPlayer.Position;
            Default.Save();
            Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            _volChanged = true;
            if (e.Delta > 0)
                Vol.Value += 0.05;
            else
                Vol.Value -= 0.05;
            e.Handled = true;
            _volChanged = false;

        }

        private void TopTitle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

        private void RoseWindow_Drop(object sender, DragEventArgs e)
        {
            var fileDrop = (string[])e.Data.GetData(DataFormats.FileDrop) ?? new string[] { };
            PlayerProperty.MusicLoaded = fileDrop[0];
            if (!fileDrop[0].EndsWith(".mp3") && !fileDrop[0].EndsWith(".flac") && !fileDrop[0].EndsWith(".wav") &&
                !fileDrop[0].EndsWith(".aac") && !fileDrop[0].EndsWith(".m4a"))
            {
                if (fileDrop[0].EndsWith(".lpl"))
                    PlayerProperty.FileExtension = PlayerProperty.Extension.PLAYLIST;
                else
                    return;
            }
            else
                PlayerProperty.FileExtension = PlayerProperty.Extension.TRACK;
            LoadMusic();
        }


        #endregion

        #region Player Control

        private void SetPlaylistItems()
        {
            for (var i = 0; i < Playlist.Items.Count; i++)
            {
                var item = (ListViewItem)Playlist.ItemContainerGenerator.ContainerFromIndex(i);
                if (item == null) continue;
                item.MouseDoubleClick -= Item_MouseDoubleClick;
                item.MouseDoubleClick += Item_MouseDoubleClick;
                item.Style = i == NowPlay ? (Style)Application.Current.Resources["PlaylistPlaying"] : null;
                if (item.ContextMenu != null) continue;
                var menu = new ContextMenu();
                var play = new MenuItem {Header = "Play"};
                play.Click += Play_Click;
                var copy = new MenuItem {Header = "Copy"};
                copy.Click += (sender, args) => Clipboard.SetText(item.Content.ToString());
                var delete = new MenuItem {Header = "Delete"};
                delete.Click += DeleteTrack_Click;
                menu.Items.Add(play);
                menu.Items.Add(copy);
                menu.Items.Add(delete);
                item.ContextMenu = menu;
                item.Template = (ControlTemplate)Application.Current.Resources["PlaylistPlayingItem"];
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e) => PlaySelected();

        public void Item_MouseDoubleClick(object sender, MouseButtonEventArgs e) => PlaySelected();

        private void PlaySelected()
        {
            if (Playlist.Items.Count > 1)
            {
                RepeatButton.IsChecked = _repeatOn = false;
                NowPlay = Playlist.SelectedIndex;
                StartTrack();
            }
            PauseButton.IsEnabled = PauseThumb.IsEnabled = true;
            PlayButton.IsEnabled = PlayThumb.IsEnabled = false;
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            _track?.Close();
            _track = new Track(TopTitle.Text, this);
            _track.Show();
        }

        private void PlayPauseControl(bool isPaused)
        {
            if (Files.Count <= 0) return;
            if (isPaused)
                MediaPlayer.Pause();
            else
                MediaPlayer.Play();
            PauseButton.IsEnabled = PauseThumb.IsEnabled = !isPaused;
            PlayButton.IsEnabled = PlayThumb.IsEnabled = _paused = isPaused;
            TaskbarItemInfo.ProgressState = isPaused ? TaskbarItemProgressState.Paused : TaskbarItemProgressState.Normal;
        }
        private void PlayButton_Click(object sender, RoutedEventArgs e) => PlayPauseControl(false);
        private void PauseButton_Click(object sender, RoutedEventArgs e) => PlayPauseControl(true);

        private void StartCommand_Executed(object sender, ExecutedRoutedEventArgs e) => PlayPauseControl(false);

        private void PauseCommand_Executed(object sender, ExecutedRoutedEventArgs e) => PlayPauseControl(true);

        private void NextPreviousControl(bool isNext)
        {
            if (Files.Count > 1)
            {
                RepeatButton.IsChecked = _repeatOn = false;
                NowPlay = NowPlay != Files.Count - 1 && isNext || NowPlay != 0 && !isNext
                    ? isNext ? NowPlay + 1 : NowPlay - 1
                    : isNext ? 0 : Files.Count - 1;
                Playlist.SelectedIndex = NowPlay;
                StartTrack();
            }
            PauseButton.IsEnabled = PauseThumb.IsEnabled = true;
            PlayButton.IsEnabled = PlayThumb.IsEnabled = false;
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
        }

        private void PreviousCommand_Executed(object sender, ExecutedRoutedEventArgs e) => NextPreviousControl(false);

        private void NextCommand_Executed(object sender, ExecutedRoutedEventArgs e) => NextPreviousControl(true);

        private void NextButton_Click(object sender, RoutedEventArgs e) => NextPreviousControl(true);

        private void PreviousButton_Click(object sender, RoutedEventArgs e) => NextPreviousControl(false);

        private void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (MuteButton.IsChecked.GetValueOrDefault())
            {
                _volChanged = true;
                _lastPos = Vol.Value;
                Vol.Value = 0;
                _volChanged = false;
            }
            else
            {
                _volChanged = true;
                Vol.Value = Math.Abs(_lastPos) > 0 ? _lastPos : 0.05;
                _volChanged = false;
            }
        }

        private void RepeatButton_Checked(object sender, RoutedEventArgs e)
        {
            if (Files.Count > 0) _repeatOn = true;
        }

        private void RepeatButton_Unchecked(object sender, RoutedEventArgs e) => _repeatOn = false;

        private void DeletePlaylist_Click(object sender, RoutedEventArgs e) => DeleteAllTracks();

        private void DeleteAllTracks()
        {
            TopTitle.Text = "Project L.A.M.A.";
            RoseWindow.Title = "Project L.A.M.A.";
            Time.Content = "        --:--";
            Visualizer.Source = new BitmapImage(_uri);
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
            MediaPlayer.Stop();
            Playlist.Items.Clear();
            Files.Clear();
            Paths.Clear();
            LastTrack = 0;
            _hasCover = false;
            NowPlay = 0;
            DeletePlaylist.Visibility = Visibility.Hidden;
            TrackTime.IsEnabled = true;
        }

        private void DeleteTrack_Click(object sender, RoutedEventArgs e)
        {
            var newTrack = Playlist.SelectedIndex == NowPlay;
            Files.RemoveAt(Playlist.SelectedIndex);
            Paths.RemoveAt(Playlist.SelectedIndex);
            Playlist.Items.RemoveAt(Playlist.SelectedIndex);
            LastTrack--;
            RepeatButton.IsChecked = _repeatOn = false;
            if (Files.Count == 0)
            {
                DeleteAllTracks();
                return;
            }
            if (!newTrack) return;
            StartTrack();
        }

        private void StartTrack()
        {
            MediaPlayer.Open(new Uri(Paths[NowPlay]));
            SetPlaylistItems();
            MediaPlayer.Play();
            TopTitle.Text = RoseWindow.Title = Files[NowPlay];
            LoadCover(NowPlay);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                PlayPauseControl(!_paused);
            else if (e.Key == Key.Down || e.Key == Key.Up)
            {
                _volChanged = true;
                Vol.Value = e.Key == Key.Down ? Vol.Value - 0.02 : Vol.Value + 0.02;
                _volChanged = false;
            }
        }

        private void Playlist_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space) PlayPauseControl(!_paused);
        }

        private void MusicControl(int i)
        {
            if (Files.Count <= 1) return;
            if (i == 0 && NowPlay != 0)
                NowPlay--;
            else if (i == 0)
                NowPlay = Files.Count - 1;
            else if (NowPlay != Files.Count - 1)
                NowPlay++;
            else
                NowPlay = 0;
            RepeatButton.IsChecked = _repeatOn = PlayButton.IsEnabled = PlayThumb.IsEnabled = false;
            Playlist.SelectedIndex = NowPlay;
            StartTrack();
            PauseButton.IsEnabled = PauseThumb.IsEnabled = true;
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            _track?.Close();
            _track = new Track(TopTitle.Text, this);
            _track.Show();
        }

        private void LoadCover(int i)
        {
            try
            {
                var file = TagLib.File.Create(Paths[i]);
                _hasCover = true;
                if (file.Tag.Pictures != null && file.Tag.Pictures.Length != 0)
                {
                    var pic = file.Tag.Pictures[0];
                    var stream = new MemoryStream(pic.Data.Data);
                    var bmp = BitmapFrame.Create(stream);
                    Visualizer.Source = bmp;
                }
                else
                    FindPicture(i);
            }
            catch (Exception ex)
            {
                File.AppendAllText("crashlog.txt", $@"{ex.Message}, {ex.StackTrace}, {DateTime.Now}");
            }
        }

        private void FindPicture(int i)
        {
            var directoryPath = Path.GetDirectoryName(Paths[i]) ?? "";
            var filteredFiles = Directory.EnumerateFiles(directoryPath)
                .Where(files => files.ToLower().EndsWith("png") || files.ToLower().EndsWith("jpg")).ToList();
            if (filteredFiles.Count > 0)
                Visualizer.Source = new BitmapImage(new Uri(filteredFiles[0]));
            else
            {
                Visualizer.Source = new BitmapImage(_uri);
                _hasCover = false;
            }
        }

        private void LoadMusic()
        {
            if (PlayerProperty.FileExtension == PlayerProperty.Extension.PLAYLIST)
            {
                LoadPlaylist(PlayerProperty.MusicLoaded);
                MediaPlayer.Open(new Uri(Paths[LastTrack]));
            }
            else
            {
                MediaPlayer.Open(new Uri(PlayerProperty.MusicLoaded));
                Files.Add(Path.GetFileNameWithoutExtension(PlayerProperty.MusicLoaded));
                Paths.Add(PlayerProperty.MusicLoaded);
            }
            SetMusic();
        }

        private void LoadPlaylist(string from)
        {
            var playlistLoaded = new List<string>();
            playlistLoaded.AddRange(File.ReadAllLines(from));
            var count = int.Parse(playlistLoaded[0]);
            for (var i = 1; i < count + 1; i++)
            {
                if (File.Exists(playlistLoaded[i]))
                {
                    Paths.Add(playlistLoaded[i]);
                    Files.Add(Path.GetFileNameWithoutExtension(playlistLoaded[i]));
                }
                else
                {
                    playlistLoaded.RemoveAt(i);
                    i--;
                    count--;
                }
            }
        }

        private void LoadLastPlaylist()
        {
            if(Default.Track != null)
            foreach (var name in Default.Track)
            {
                if (!File.Exists(new FileInfo(name).FullName)) continue;
                Files.Add(Path.GetFileNameWithoutExtension(name));
                Paths.Add(name);
                Playlist.Items.Add(Path.GetFileNameWithoutExtension(name));
            }
            if (Paths.Capacity <= 0) return;
            try
            {
                StartLoadedPlaylist();
            }
            catch (Exception ex)
            {
                File.AppendAllText("crashlog.txt", $@"{ex.Message}, {ex.StackTrace}, {DateTime.Now}");
            }
        }

        private void StartLoadedPlaylist()
        {
            NowPlay = Default.NowPlay;
            TrackTime.IsEnabled = true;
            MediaPlayer.Open(new Uri(Paths[NowPlay]));
            LoadCover(NowPlay);
            TopTitle.Text = RoseWindow.Title = Files[NowPlay];
            if (Playlist.Items.Count > 0)
            {
                Playlist.SelectedIndex = NowPlay;
                MediaPlayer.Position = Default.Time;
                MediaPlayer.Pause();
                DeletePlaylist.Visibility = Visibility.Visible;
                PauseButton.IsEnabled = PauseThumb.IsEnabled = false;
                PlayButton.IsEnabled = PlayThumb.IsEnabled = true;
            }
            LastTrack = Files.Count;
            NextButton.IsEnabled = PreviousButton.IsEnabled = NextThumb.IsEnabled = BackThumb.IsEnabled = Files.Count > 1;
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Paused;
            Default.Track.Clear();
            Default.NowPlay = 0;
            Default.Time = TimeSpan.Zero;
        }

        #endregion

        #region Sliders
        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (Paths.Count == 0)
                    NextButton.IsEnabled = PauseButton.IsEnabled = PlayButton.IsEnabled = PreviousButton.IsEnabled = RepeatButton.IsEnabled = false;
                else
                    RepeatButton.IsEnabled = true;
                if (_activeWindow == 0) _activeWindow = GetForegroundWindow();
                var windowState = GetForegroundWindow();
                if (GetAsyncKeyState(0xB3) && _keyTimer == 0) PlayPauseControl(!_paused);
                else if (GetAsyncKeyState(0xB1) && _keyTimer == 0 && windowState != _activeWindow) MusicControl(0);
                else if (GetAsyncKeyState(0xB0) && _keyTimer == 0 && windowState != _activeWindow) MusicControl(1);
                if(_keyTimer == 0) _keyTimer = 1;
                if (MediaPlayer.Source != null && Paths.Count != 0 && MediaPlayer.NaturalDuration.HasTimeSpan)
                    Time.Content = $"{MediaPlayer.Position:mm\\:ss} / {MediaPlayer.NaturalDuration.TimeSpan:mm\\:ss}";
                else if (Paths.Count == 0)
                {
                    Time.Content = "        --:--";
                    TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                }
                if (MediaPlayer.Source != null && MediaPlayer.NaturalDuration.HasTimeSpan && !_userIsDraggingSlider)
                    SetTrackTime();
                if (_keyTimer != 0) _keyTimer--;
            }
            catch (Exception ex)
            {
                File.AppendAllText("crashlog.txt", $@"{ex.Message}, {ex.StackTrace}, {DateTime.Now}");
            }
        }

        private void SetTrackTime()
        {
            TrackTime.Minimum = 0;
            TrackTime.Maximum = MediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
            TrackTime.Value = MediaPlayer.Position.TotalSeconds;
            TaskbarItemInfo.ProgressValue = TrackTime.Value / TrackTime.Maximum;
            if (Math.Abs(TrackTime.Value - MediaPlayer.NaturalDuration.TimeSpan.TotalSeconds) != 0) return;
            if (Files.Count >= 1)
            {
                if (_repeatOn == false)
                {
                    if (NowPlay != Files.Count - 1)
                        NowPlay++;
                    else
                        NowPlay = 0;
                    Playlist.SelectedIndex = NowPlay;
                    StartTrack();
                }
                else
                    MediaPlayer.Open(new Uri(Paths[NowPlay]));
                _track?.Close();
                _track = new Track(TopTitle.Text, this);
                _track.Show();
            }
            PauseButton.IsEnabled = PauseThumb.IsEnabled = true;
            PlayButton.IsEnabled = PlayThumb.IsEnabled = false;
        }

        private void TrackTime_DragStarted(object sender, DragStartedEventArgs e) => _userIsDraggingSlider = true;

        private void TrackTime_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            _userIsDraggingSlider = false;
            MediaPlayer.Position = TimeSpan.FromSeconds(TrackTime.Value);
        }

        private void TrackTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => TrackTime.SelectionEnd = TrackTime.Value;

        private void Vol_DragStarted(object sender, RoutedEventArgs e) => _volChanged = true;

        private void Vol_DragCompleted(object sender, RoutedEventArgs e) => _volChanged = false;

        private void Vol_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (!PlayerProperty.Start && _volChanged)
                    MediaPlayer.Volume = Vol.Value;
                else
                {
                    PlayerProperty.Start = false;
                    Vol.Value = _volChange;
                    MediaPlayer.Volume = _volChange;
                }
                MuteButton.IsChecked = Vol.Value <= 0;
            }
            catch (Exception ex)
            {
                File.AppendAllText("crashlog.txt", $@"{ex.Message}, {ex.StackTrace}, {DateTime.Now}");
            }
        }
        #endregion

        #region Menu Control
        private void OpenFile_Selected(object sender, RoutedEventArgs e)
        {
            try
            {
                RepeatButton.IsChecked = _repeatOn = false;
                var ofd = new OpenFileDialog
                {
                    Multiselect = true,
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                    Filter =
                        "All supported files (*.mp3;*.wav;*.aac;*.flac;*.m4a)|*.mp3;*.wav;*.aac;*.flac;*.m4a|MP3 files (*.mp3)|*.mp3|WAV files (*.wav)|*.wav|AAC files (*.aac)|*.aac|Lossless files (*.flac;*.m4a)|*.flac;*.m4a"
                };
                if (ofd.ShowDialog() == true)
                    MediaPlayer.Open(new Uri(ofd.FileName));
                ofd.FileNames.ToList().ForEach(f => Files.Add(Path.GetFileNameWithoutExtension(f)));
                Paths.AddRange(ofd.FileNames);
                SetMusic();
            }
            catch (Exception ex)
            {
                File.AppendAllText("crashlog.txt", $@"{ex.Message}, {ex.StackTrace}, {DateTime.Now}");
            }
        }

        private void OpenPlaylist_Selected(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                    Filter = "LAMA playlist|*.lpl"
                };
                if (openFileDialog.ShowDialog() != true) return;
                Files.Clear();
                Paths.Clear();
                RepeatButton.IsChecked = _repeatOn = false;
                Playlist.Items.Clear();
                LastTrack = 0;
                LoadPlaylist(openFileDialog.FileName);
                MediaPlayer.Open(new Uri(Paths[0]));
                SetMusic();
            }
            catch (Exception ex)
            {
                File.AppendAllText("crashlog.txt", $@"{ex.Message}, {ex.StackTrace}, {DateTime.Now}");
            }
        }

        private void SetMusic()
        {
            for (var i = LastTrack; i < Files.Count; i++)
            {
                Playlist.Items.Add(Files[i]);
                if (i != LastTrack) continue;
                LoadCover(i);
                TopTitle.Text = RoseWindow.Title = Files[i];
            }
            if (Playlist.Items.Count > 0)
            {
                Playlist.SelectedIndex = LastTrack;
                TrackTime.IsEnabled = true;
                MediaPlayer.Play();
                DeletePlaylist.Visibility = Visibility.Visible;
            }
            NowPlay = LastTrack;
            LastTrack = Files.Count;
            NextButton.IsEnabled = PreviousButton.IsEnabled = NextThumb.IsEnabled = BackThumb.IsEnabled = Files.Count > 1;
            PauseButton.IsEnabled = PauseThumb.IsEnabled = true;
            PlayButton.IsEnabled = PlayThumb.IsEnabled = false;
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
        }

        private void Save_Selected(object sender, RoutedEventArgs e)
        {
            if (Files.Count <= 0) return;
            var saveFileDialog = new SaveFileDialog
            {
                FileName = "Playlist",
                Filter = "LAMA playlist|*.lpl",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
            };
            if (saveFileDialog.ShowDialog() != true) return;
            var saveFile = new List<string> { Paths.Count.ToString() };
            saveFile.AddRange(Paths);
            File.WriteAllLines(saveFileDialog.FileName, saveFile);
        }

        public void SetTheme(PlayerTheme theme)
        {
            _uri = new Uri(UriAttribute.Get(theme), UriKind.Relative);
            if (!_hasCover)
                Visualizer.Source = new BitmapImage(_uri);
            Default.Theme = (int)theme;
            Resources = new ResourceDictionary { Source = new Uri($"pack://application:,,,/Themes/{ResourceAttribute.Get(theme)}") };
        }

        private void RoseThemeButton_Click(object sender, RoutedEventArgs e) => SetTheme(PlayerTheme.ROSE);

        private void BlueThemeButton_Click(object sender, RoutedEventArgs e) => SetTheme(PlayerTheme.BLUE);

        private void RedThemeButton_Click(object sender, RoutedEventArgs e) => SetTheme(PlayerTheme.RED);

        private void GreenThemeButton_Click(object sender, RoutedEventArgs e) => SetTheme(PlayerTheme.GREEN);

        private void GrayThemeButton_Click(object sender, RoutedEventArgs e) => SetTheme(PlayerTheme.GRAY);
        #endregion
    }
}