using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ProjectLama
{

    partial class Track : Window
    {
        private readonly Window _window;
        public Track(string track, Window window)
        {
            InitializeComponent();
            Theme();

            TrackPlaying.Text = track;
            var desktopWorkingArea = SystemParameters.WorkArea;
            Left = desktopWorkingArea.Right - Width - 8;
            Top = desktopWorkingArea.Bottom - Height - 8;
            _window = window;
            StartCloseTimer();
        }

        private void Theme()
        {
            SetTheme(Properties.Settings.Default.Theme);
        }

        public void SetTheme(int themeIndex)
        {
            var theme = (PlayerTheme)themeIndex;
            LamaPic.Source = new BitmapImage(new Uri(UriAttribute.Get(theme), UriKind.Relative));
            Resources = new ResourceDictionary { Source = new Uri($"pack://application:,,,/{ResourceAttribute.Get(theme)}") };
        }

        private void StartCloseTimer()
        {
            var timer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(3d)};
            timer.Tick += TimerTick;
            timer.Start();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            var timer = (DispatcherTimer)sender;
            timer.Stop();
            timer.Tick -= TimerTick;
            Close();
        }

        private void Notification_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _window.Activate();
            _window.WindowState = WindowState.Normal;
            Close();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
