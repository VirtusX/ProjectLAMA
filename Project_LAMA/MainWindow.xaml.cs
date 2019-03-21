using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;
using Microsoft.Win32;

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
        public int StartProgram; //I dunno, why I did it, but without this volume is loaded incorrectly
        private readonly double _volChange; // And this
        private bool _volChanged; // Yes-yes-yes
        private bool _repeatOn;
        private int _fileExt;
        private string _musicLoaded;
        private Uri _uri = new Uri(@"\Assets\LamaRose.png", UriKind.Relative);
        private bool _hasCover;
        private int _keyTimer;
        private Track _track;
        private int _activeWindow;

        [DllImport("user32.dll")]
        private static extern bool GetAsyncKeyState(int key);
        [DllImport("user32.dll")]
        private static extern int GetForegroundWindow();
        #endregion

        public MainWindow(int theme = 0, double vol = 1, int start = 0, string musicLoad = null, int fileExt = 0, bool hasPlaylist = false)
        {
            InitializeComponent();
            SetTheme((PlayerTheme)theme);
            var timer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(1)};
            timer.Tick += Timer_Tick;
            timer.Start();
            _fileExt = fileExt;
            _musicLoaded = musicLoad;
            Visualizer.Source = new BitmapImage(_uri);
            if (_musicLoaded != null)
                LoadMusic();
            else if (hasPlaylist)
                LoadPlaylist();
            StartProgram = start;
            Vol.Value = MediaPlayer.Volume = _volChange = vol;
            Playlist.SelectedIndex = Playlist.Items.Count > 0 ? NowPlay : 0;
        }

        #region Window Control
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Track = new List<string>();
            if (Paths.Count > 0) Properties.Settings.Default.Track.AddRange(Paths);
            Properties.Settings.Default.NowPlay = NowPlay;
            Properties.Settings.Default.Volume = MediaPlayer.Volume;
            Properties.Settings.Default.Time = MediaPlayer.Position;
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
                _volChanged = true;
                if (e.Delta > 0)
                    Vol.Value += 0.05;
                else
                    Vol.Value -= 0.05;
                e.Handled = true;
                _volChanged = false;
            }
            else {
                if (e.Delta > 0)
                    ThemeChooser.LineUp();
                else
                    ThemeChooser.LineDown();
                e.Handled = true;
            }

        }

        private void TopTitle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void RoseWindow_Drop(object sender, DragEventArgs e)
        {
            var fileDrop = (string[])e.Data.GetData(DataFormats.FileDrop);
            _musicLoaded = fileDrop[0];
            if (fileDrop[0].EndsWith(".mp3") || fileDrop[0].EndsWith(".flac") || fileDrop[0].EndsWith(".wav") || fileDrop[0].EndsWith(".aac") || fileDrop[0].EndsWith(".m4a"))
            {
                _fileExt = 2;
                LoadMusic();
            }
            else if (fileDrop[0].EndsWith(".lpl"))
            {
                _fileExt = 1;
                LoadMusic();
            }
        }


        #endregion

        #region Player Control
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
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            PlayPauseControl(false);
        }
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            PlayPauseControl(true);
        }

        private void StartCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            PlayPauseControl(false);
        }

        private void PauseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            PlayPauseControl(true);
        }

        private void NextPreviousControl(bool isNext)
        {
            if (Files.Count > 1)
            {
                RepeatButton.IsChecked = _repeatOn = false;
                ;
                if (NowPlay != Files.Count - 1 && isNext || NowPlay != 0 && !isNext)
                {
                    if(isNext)
                        NowPlay++;
                    else
                        NowPlay--;
                    Playlist.SelectedIndex = NowPlay;
                }
                else
                {
                    NowPlay = isNext ? 0 : Files.Count - 1;
                    Playlist.SelectedIndex = NowPlay;
                }
                MediaPlayer.Open(new Uri(Paths[NowPlay]));
                MediaPlayer.Play();
                TopTitle.Text = Files[NowPlay];
                RoseWindow.Title = Files[NowPlay];
                LoadCover(NowPlay);
            }
            PauseButton.IsEnabled = PauseThumb.IsEnabled = true;
            PlayButton.IsEnabled = PlayThumb.IsEnabled = false;
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
        }

        private void PreviousCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
           NextPreviousControl(false);
        }

        private void NextCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            NextPreviousControl(true);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            NextPreviousControl(true);
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            NextPreviousControl(false);
        }

        private void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            switch (MuteButton.IsChecked)
            {
                case true:
                    _volChanged = true;
                    _lastPos = Vol.Value;
                    Vol.Value = 0;
                    _volChanged = false;
                    break;
                case false:
                    _volChanged = true;
                    Vol.Value = Math.Abs(_lastPos) > 0 ? _lastPos : 0.05;
                    _volChanged = false;
                    break;
            }
        }

        private void Playlist_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Playlist.Items.Count > 1)
            {
                RepeatButton.IsChecked = false;
                _repeatOn = false;
                    MediaPlayer.Open(new Uri(Paths[Playlist.SelectedIndex]));
                    MediaPlayer.Play();
                    TopTitle.Text = Files[Playlist.SelectedIndex];
                    RoseWindow.Title = Files[Playlist.SelectedIndex];
                    NowPlay = Playlist.SelectedIndex;
                    LoadCover(NowPlay);
            }
            PauseButton.IsEnabled = PauseThumb.IsEnabled = true;
            PlayButton.IsEnabled = PlayThumb.IsEnabled = false;
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            _track?.Close();
            _track = new Track(TopTitle.Text, this);
            _track.Show();
        }

        private void RepeatButton_Checked(object sender, RoutedEventArgs e)
        {
            if (Files.Count > 0) _repeatOn = true;
        }

        private void RepeatButton_Unchecked(object sender, RoutedEventArgs e)
        {
            _repeatOn = false;
        }

        private void DeletePlaylist_Click(object sender, RoutedEventArgs e)
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
        }

        private void DeleteTrack_Click(object sender, RoutedEventArgs e)
        {
            if (Files.Count <= 1 || Playlist.SelectedIndex < 0) return;
            var newTrack = Playlist.SelectedIndex == NowPlay;
            Files.RemoveAt(Playlist.SelectedIndex);
            Paths.RemoveAt(Playlist.SelectedIndex);
            Playlist.Items.RemoveAt(Playlist.SelectedIndex);
            LastTrack--;
            RepeatButton.IsChecked = _repeatOn = false;
            if (!newTrack) return;
            MediaPlayer.Open(new Uri(Paths[NowPlay]));
            MediaPlayer.Play();
            TopTitle.Text = Files[NowPlay];
            RoseWindow.Title = Files[NowPlay];
            LoadCover(NowPlay);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Space:
                    MusicControl(1);
                    break;
                case Key.Down:
                    _volChanged = true;
                    Vol.Value -= 0.02;
                    _volChanged = false;
                    break;
                case Key.Up:
                    _volChanged = true;
                    Vol.Value += 0.02;
                    _volChanged = false;
                    break;
            }
        }

        private void Playlist_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                MusicControl(1);
        }

        private void MusicControl(int i)
        {
            switch (i)
            {
                case 1:
                {
                    PlayPauseControl(!_paused);
                    break;
                }
                case 2:
                {
                    if (Files.Count <= 1) break;
                    RepeatButton.IsChecked = _repeatOn = false;
                    if (NowPlay != 0)
                    {
                        NowPlay--;
                        Playlist.SelectedIndex = NowPlay;
                    }
                    else
                    {
                        NowPlay = Files.Count - 1;
                        Playlist.SelectedIndex = NowPlay;
                    }

                    MediaPlayer.Open(new Uri(Paths[NowPlay]));
                    MediaPlayer.Play();
                    TopTitle.Text = Files[NowPlay];
                    RoseWindow.Title = Files[NowPlay];
                    LoadCover(NowPlay);
                    PauseButton.IsEnabled = PauseThumb.IsEnabled = true;
                    PlayButton.IsEnabled = PlayThumb.IsEnabled = false;
                    TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                    _track?.Close();
                    _track = new Track(TopTitle.Text, this);
                    _track.Show();

                    break;
                }
                case 3:
                {
                    if (Files.Count <= 1) break;
                    RepeatButton.IsChecked = _repeatOn = false;
                    if (NowPlay != Files.Count - 1)
                    {
                        NowPlay++;
                        Playlist.SelectedIndex = NowPlay;
                    }
                    else
                    {
                        NowPlay = 0;
                        Playlist.SelectedIndex = NowPlay;
                    }

                    MediaPlayer.Open(new Uri(Paths[NowPlay]));
                    MediaPlayer.Play();
                    TopTitle.Text = Files[NowPlay];
                    RoseWindow.Title = Files[NowPlay];
                    LoadCover(NowPlay);
                    PauseButton.IsEnabled = PauseThumb.IsEnabled = true;
                    PlayButton.IsEnabled = PlayThumb.IsEnabled = false;
                    TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                    _track?.Close();
                    _track = new Track(TopTitle.Text, this);
                    _track.Show();
                    break;
                }
            }
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
                {
                    var directoryPath = Path.GetDirectoryName(Paths[i]);
                    var filteredFiles = Directory
                        .EnumerateFiles(directoryPath) //<--- .NET 4.5
                        .Where(files => files.ToLower().EndsWith("png") || files.ToLower().EndsWith("jpg"))
                        .ToList();
                    if (filteredFiles.Count > 0)
                        Visualizer.Source = new BitmapImage(new Uri(filteredFiles[0]));
                    else
                    {
                        Visualizer.Source = new BitmapImage(_uri);
                        _hasCover = false;
                    }
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("crashlog.txt", ex.Message);
            }
        }

        private void LoadMusic()
        {
            if (_fileExt == 1)
            {
                var playlistLoaded = new List<string>();
                playlistLoaded.AddRange(File.ReadAllLines(_musicLoaded));
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
                MediaPlayer.Open(new Uri(Paths[LastTrack]));
            }
            else
            {
                MediaPlayer.Open(new Uri(_musicLoaded));
                Files.Add(Path.GetFileNameWithoutExtension(_musicLoaded));
                Paths.Add(_musicLoaded);
            }
            SetMusic();
        }

        private void LoadPlaylist()
        {
            foreach (var name in Properties.Settings.Default.Track)
            {
                if (!File.Exists(new FileInfo(name).FullName)) continue;
                Files.Add(Path.GetFileNameWithoutExtension(name));
                Paths.Add(name);
                Playlist.Items.Add(Path.GetFileNameWithoutExtension(name));
            }

            if (Paths.Capacity <= 0) return;
            try
            {
                MediaPlayer.Open(new Uri(Paths[Properties.Settings.Default.NowPlay]));
                LoadCover(Properties.Settings.Default.NowPlay);
                TopTitle.Text = Files[Properties.Settings.Default.NowPlay];
                RoseWindow.Title = Files[Properties.Settings.Default.NowPlay];
                if (Playlist.Items.Count > 0)
                {
                    Playlist.SelectedIndex = Properties.Settings.Default.NowPlay;
                    MediaPlayer.Position = Properties.Settings.Default.Time;
                    MediaPlayer.Pause();
                }
                NowPlay = Properties.Settings.Default.NowPlay;
                LastTrack = Files.Count;     
                NextButton.IsEnabled = PreviousButton.IsEnabled = NextThumb.IsEnabled = BackThumb.IsEnabled = Files.Count > 1;
                PauseButton.IsEnabled = PauseThumb.IsEnabled = true;
                PlayButton.IsEnabled = PlayThumb.IsEnabled = false;
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Paused;
                Properties.Settings.Default.Track.Clear();
                Properties.Settings.Default.NowPlay = 0;
                Properties.Settings.Default.Time = TimeSpan.Zero;
            }
            catch(Exception e)
            {
                File.AppendAllText("Exception.txt", e.Message);
            }
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
                if(_activeWindow == 0) _activeWindow = GetForegroundWindow();
                var windowState = GetForegroundWindow();
                if (GetAsyncKeyState(0xB3) && _keyTimer == 0)
                {
                    MusicControl(1);
                    _keyTimer = 1;
                }
                if (GetAsyncKeyState(0xB1) && _keyTimer == 0 && windowState != _activeWindow)
                {
                    MusicControl(2);
                    _keyTimer = 1;
                }
                if (GetAsyncKeyState(0xB0) && _keyTimer == 0 && windowState != _activeWindow)
                {
                    MusicControl(3);
                    _keyTimer = 1;
                }
                if (MediaPlayer.Source != null && Paths.Count != 0)
                    Time.Content = $"{MediaPlayer.Position:mm\\:ss} / {MediaPlayer.NaturalDuration.TimeSpan:mm\\:ss}";
                else if(Paths.Count == 0)
                {
                    Time.Content = "        --:--";
                    TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                }
                if (MediaPlayer.Source != null && (MediaPlayer.NaturalDuration.HasTimeSpan) && !_userIsDraggingSlider)
                {
                    TrackTime.Minimum = 0;
                    TrackTime.Maximum = MediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                    TrackTime.Value = MediaPlayer.Position.TotalSeconds;
                    TaskbarItemInfo.ProgressValue = TrackTime.Value / TrackTime.Maximum;
                    if (Math.Abs(TrackTime.Value - MediaPlayer.NaturalDuration.TimeSpan.TotalSeconds) < 0)
                    {                       
                        if (Files.Count >= 1)
                        {
                            if (_repeatOn == false)
                            {
                                if (NowPlay != Files.Count - 1)
                                {
                                    NowPlay++;
                                    Playlist.SelectedIndex = NowPlay;
                                }
                                else
                                {
                                    NowPlay = 0;
                                    Playlist.SelectedIndex = NowPlay;
                                }
                                MediaPlayer.Open(new Uri(Paths[NowPlay]));
                                MediaPlayer.Play();
                                TopTitle.Text = Files[NowPlay];
                                RoseWindow.Title = Files[NowPlay];
                                LoadCover(NowPlay); 
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
                }
                if(_keyTimer!=0)
                    _keyTimer--;
            }
            catch { }
        }

        private void TrackTime_DragStarted(object sender, DragStartedEventArgs e)
        {
            _userIsDraggingSlider = true;
        }

        private void TrackTime_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            _userIsDraggingSlider = false;
            MediaPlayer.Position = TimeSpan.FromSeconds(TrackTime.Value);
        }

        private void TrackTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TrackTime.SelectionEnd = TrackTime.Value;
        }

        private void Vol_DragStarted(object sender, RoutedEventArgs e)
        {
            _volChanged = true;
        }

        private void Vol_DragCompleted(object sender, RoutedEventArgs e)
        {
            _volChanged = false;
        }

        private void Vol_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (StartProgram == 0 && _volChanged)
                    MediaPlayer.Volume = Vol.Value;
                else
                {
                    StartProgram = 0;
                    Vol.Value = _volChange;
                    MediaPlayer.Volume = _volChange;
                }
                MuteButton.IsChecked = Vol.Value == 0 || Vol.Value < 0;
            }
            catch { };
        }
        #endregion

        #region Menu Control
        private void HamburgerButton_Checked(object sender, RoutedEventArgs e)
        {
            Menu.Visibility = Visibility.Visible;
            OpenFile.IsSelected = OpenPlaylist.IsSelected = Save.IsSelected = Themes.IsSelected = false;
        }

        private void HamburgerButton_Unchecked(object sender, RoutedEventArgs e)
        {
            Menu.Visibility = Visibility.Hidden;
        }

        private void OpenFile_Selected(object sender, RoutedEventArgs e)
        {
            try
            {
                HamburgerButton.IsChecked = RepeatButton.IsChecked = _repeatOn = false;
                Menu.Visibility = Visibility.Hidden;
                var openFileDialog = new OpenFileDialog
                {
                    Multiselect = true,
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                    Filter =
                        "All supported files (*.mp3;*.wav;*.aac;*.flac;*.m4a)|*.mp3;*.wav;*.aac;*.flac;*.m4a|MP3 files (*.mp3)|*.mp3|WAV files (*.wav)|*.wav|AAC files (*.aac)|*.aac|Lossless files (*.flac;*.m4a)|*.flac;*.m4a"
                };
                if (openFileDialog.ShowDialog() == true)
                    MediaPlayer.Open(new Uri(openFileDialog.FileName));                
                foreach (var file in openFileDialog.FileNames)
                    Files.Add(Path.GetFileNameWithoutExtension(file));
                Paths.AddRange(openFileDialog.FileNames);
                SetMusic();
            }
            catch (Exception ex)
            {
                File.AppendAllText("crashlog.txt", ex.Message);
            }
        }

        private void OpenPlaylist_Selected(object sender, RoutedEventArgs e)
        {
            try
            {
                HamburgerButton.IsChecked = false;
                Menu.Visibility = Visibility.Hidden;
                var openFileDialog = new OpenFileDialog
                {
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                    Filter = "LAMA playlist|*.lpl"
                };
                if (openFileDialog.ShowDialog() != true) return;
                var playlistLoaded = new List<string>();
                playlistLoaded.AddRange(File.ReadAllLines(openFileDialog.FileName));
                var count = int.Parse(playlistLoaded[0]);
                Files.Clear();
                Paths.Clear();
                RepeatButton.IsChecked = _repeatOn = false;
                Playlist.Items.Clear();
                LastTrack = 0;
                for (var i = 1; i <= count; i++)
                {
                    if (File.Exists(new FileInfo(playlistLoaded[i]).FullName))
                    {
                        Files.Add(Path.GetFileNameWithoutExtension(playlistLoaded[i]));
                        Paths.Add(playlistLoaded[i]);
                    }
                    else
                    {
                        playlistLoaded.RemoveAt(i);
                        i--;
                        count--;
                    }
                }
                MediaPlayer.Open(new Uri(Paths[0]));
                SetMusic();
            }
            catch (Exception ex)
            {
                File.AppendAllText("crashlog.txt", ex.Message);
            }
        }

        private void SetMusic()
        {
            for (var i = LastTrack; i < Files.Count; i++)
            {
                Playlist.Items.Add(Files[i]);
                if (i != LastTrack) continue;
                LoadCover(i);
                TopTitle.Text = Files[i];
                RoseWindow.Title = Files[i];
            }
            if (Playlist.Items.Count > 0)
            {
                Playlist.SelectedIndex = LastTrack;
                MediaPlayer.Play();
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
            if (saveFileDialog.ShowDialog() == true)
            {
                var saveFile = new List<string> {Paths.Count.ToString()};
                saveFile.AddRange(Paths);
                File.WriteAllLines(saveFileDialog.FileName, saveFile);
            }
            HamburgerButton.IsChecked = false;
            Menu.Visibility = Visibility.Hidden;
        }

        private void Close_Selected(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Theme = 0;
            if (Paths.Count > 0)
            {
                Properties.Settings.Default.Track = new List<string>();
                Properties.Settings.Default.Track.AddRange(Paths);
            }
            Properties.Settings.Default.NowPlay = NowPlay;
            Properties.Settings.Default.Volume = MediaPlayer.Volume;
            Properties.Settings.Default.Time = MediaPlayer.Position;
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

        public void SetTheme(PlayerTheme theme)
        {
            _uri = new Uri(UriAttribute.Get(theme), UriKind.Relative);
            if (!_hasCover)
                Visualizer.Source = new BitmapImage(_uri);
            Properties.Settings.Default.Theme = (int)theme;
            Resources = new ResourceDictionary { Source = new Uri($"pack://application:,,,/{ResourceAttribute.Get(theme)}")};
        }

        private void RoseThemeButton_Click(object sender, RoutedEventArgs e)
        {
            SetTheme(PlayerTheme.ROSE);
        }

        private void BlueThemeButton_Click(object sender, RoutedEventArgs e)
        {
            SetTheme(PlayerTheme.BLUE);
        }

        private void RedThemeButton_Click(object sender, RoutedEventArgs e)
        {
            SetTheme(PlayerTheme.RED);
        }

        private void GreenThemeButton_Click(object sender, RoutedEventArgs e)
        {
            SetTheme(PlayerTheme.GREEN);
        }

        private void GrayThemeButton_Click(object sender, RoutedEventArgs e)
        {
            SetTheme(PlayerTheme.GRAY);
        }
        #endregion

    }
}
