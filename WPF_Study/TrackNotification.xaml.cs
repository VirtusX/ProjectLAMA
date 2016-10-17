using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WPF_Study
{
    /// <summary>
    /// Interaction logic for Track.xaml
    /// </summary>
    public partial class Track : Window
    {
        Window window;
        public Track(string track, Window window)
        {
            InitializeComponent();
            Theme();

            TrackPlaying.Text = track;
            var desktopWorkingArea = SystemParameters.WorkArea;
            Left = desktopWorkingArea.Right - Width - 8;
            Top = desktopWorkingArea.Bottom - Height - 8;
            this.window = window;
            StartCloseTimer();
        }

        private void Theme()
        {
            Uri uri;
            switch (Properties.Settings.Default.Theme)
            {
                case 0:
                    uri = new Uri(@"\Assets\LamaRose.png", UriKind.Relative);
                    LamaPic.Source = new BitmapImage(uri);
                    Resources = new ResourceDictionary() { Source = new Uri("pack://application:,,,/RoseDictionary.xaml") };
                    break;
                case 1:
                    uri = new Uri(@"\Assets\LamaBlue.png", UriKind.Relative);
                    LamaPic.Source = new BitmapImage(uri);
                    Resources = new ResourceDictionary() { Source = new Uri("pack://application:,,,/BlueDictionary.xaml") };
                    break;
                case 2:
                    uri = new Uri(@"\Assets\LamaRed.png", UriKind.Relative);
                    LamaPic.Source = new BitmapImage(uri);
                    Resources = new ResourceDictionary() { Source = new Uri("pack://application:,,,/CrimsonDictionary.xaml") };
                    break;
                case 3:
                    uri = new Uri(@"\Assets\LamaGreen.png", UriKind.Relative);
                    LamaPic.Source = new BitmapImage(uri);
                    Resources = new ResourceDictionary() { Source = new Uri("pack://application:,,,/GreenDictionary.xaml") };
                    break;
                case 4:
                    uri = new Uri(@"\Assets\LamaGrey.png", UriKind.Relative);
                    LamaPic.Source = new BitmapImage(uri);
                    Resources = new ResourceDictionary() { Source = new Uri("pack://application:,,,/GreyDictionary.xaml") };
                    break;
            }
            
        }

        private void StartCloseTimer()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3d);
            timer.Tick += TimerTick;
            timer.Start();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            DispatcherTimer timer = (DispatcherTimer)sender;
            timer.Stop();
            timer.Tick -= TimerTick;
            Close();
        }

        private void Notification_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            window.Activate();
            window.WindowState = WindowState.Normal;
            Close();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
