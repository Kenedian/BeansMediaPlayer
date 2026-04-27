using BeansMediaPlayer.Models;
using BeansMediaPlayer.Services;
using LibVLCSharp.Shared;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace BeansMediaPlayer.Views
{
    public partial class PlayerView : System.Windows.Controls.UserControl
    {
        private readonly LibVLC _libVLC;
        private readonly MediaPlayer _mediaPlayer;
        private readonly TimeSpan? _startPosition;

        private readonly DispatcherTimer _uiTimer;
        private readonly DispatcherTimer _resumeTimer;
        private readonly DispatcherTimer _controlsHideTimer;

        private bool _isDraggingSlider;

        private readonly ImportedSeries _series;
        private readonly Episode _episode;
        private readonly SeriesStorageService _storageService = new();

        private bool _autoplayEnabled = true;
        private bool _autoplayTriggered;

        public PlayerView(
            ImportedSeries series,
            Episode episode,
            TimeSpan? startPosition = null)
        {
            InitializeComponent();

            _series = series;
            _episode = episode;
            _startPosition = startPosition;

            AutoplayCheckBox.IsChecked = _autoplayEnabled;

            Loaded += (_, _) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    Keyboard.Focus(InputOverlay);
                    ShowControls();
                }), DispatcherPriority.ApplicationIdle);
            };

            Core.Initialize();

            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);

            VideoPlayer.MediaPlayer = _mediaPlayer;

            _mediaPlayer.Volume = _series.Volume;
            VolumeSlider.Value = _series.Volume;

            UpdateEpisodeInfo();

            _mediaPlayer.Playing += MediaPlayer_Playing;
            _mediaPlayer.Play(new Media(_libVLC, episode.FilePath));

            _uiTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _uiTimer.Tick += UiTimer_Tick;
            _uiTimer.Start();

            _resumeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(15)
            };
            _resumeTimer.Tick += ResumeTimer_Tick;

            _controlsHideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _controlsHideTimer.Tick += ControlsHideTimer_Tick;
            _controlsHideTimer.Start();
        }

        private void InputOverlay_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ShowControls();
        }

        private void ShowControls()
        {
            ControlsOverlay.Visibility = Visibility.Visible;
            MenuButton.Visibility = Visibility.Visible;
            Mouse.OverrideCursor = null;

            _controlsHideTimer.Stop();
            _controlsHideTimer.Start();
        }

        private void ControlsHideTimer_Tick(object? sender, EventArgs e)
        {
            _controlsHideTimer.Stop();

            if (!_mediaPlayer.IsPlaying)
                return;

            ControlsOverlay.Visibility = Visibility.Collapsed;
            MenuButton.Visibility = Visibility.Collapsed;
            Mouse.OverrideCursor = System.Windows.Input.Cursors.None;
        }

        private void AutoplayCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            _autoplayEnabled = AutoplayCheckBox.IsChecked == true;
        }

        private void RestartResumeTimer()
        {
            _resumeTimer.Stop();
            _resumeTimer.Start();
        }

        private void ResumeTimer_Tick(object? sender, EventArgs e)
        {
            _resumeTimer.Stop();
            SaveResumePosition();
        }

        private void SaveResumePosition()
        {
            if (_mediaPlayer.Time <= 0)
                return;

            var season = _series.Seasons
                .FirstOrDefault(s => s.Episodes.Contains(_episode));

            if (season is null)
                return;

            _series.Resume = new ResumeData
            {
                HasResume = true,
                Season = season.Number,
                Episode = _episode.Number,
                Position = TimeSpan.FromMilliseconds(_mediaPlayer.Time)
            };

            _storageService.SaveSeries(_series);
        }

        private void InputOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.Focus(InputOverlay);
            ShowControls();
        }

        private void InputOverlay_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ShowControls();

            switch (e.Key)
            {
                case Key.Space:
                    TogglePlayPause();
                    break;

                case Key.Left:
                case Key.A:
                    SeekRelative(-10);
                    break;

                case Key.Right:
                case Key.D:
                    SeekRelative(10);
                    break;

                case Key.Up:
                case Key.W:
                    ChangeVolume(5);
                    break;

                case Key.Down:
                case Key.S:
                    ChangeVolume(-5);
                    break;

                case Key.Escape:
                    MenuButton_Click(sender, new RoutedEventArgs());
                    break;

                default:
                    return;
            }

            e.Handled = true;
        }

        private void ChangeVolume(int delta)
        {
            int newVolume = Math.Clamp(_mediaPlayer.Volume + delta, 0, 100);

            _mediaPlayer.Volume = newVolume;
            VolumeSlider.Value = newVolume;

            _series.Volume = newVolume;
            _storageService.SaveSeries(_series);
        }

        private void UiTimer_Tick(object? sender, EventArgs e)
        {
            if (_mediaPlayer.Length <= 0)
                return;

            if (!_isDraggingSlider)
            {
                ProgressSlider.Maximum = _mediaPlayer.Length;
                ProgressSlider.Value = _mediaPlayer.Time;
            }

            TimeText.Text =
                $"{TimeSpan.FromMilliseconds(_mediaPlayer.Time):hh\\:mm\\:ss} / " +
                $"{TimeSpan.FromMilliseconds(_mediaPlayer.Length):hh\\:mm\\:ss}";

            HandleAutoplay();
        }

        private void HandleAutoplay()
        {
            if (!_autoplayEnabled || _autoplayTriggered || _mediaPlayer.Length <= 0)
                return;

            long remainingMs = _mediaPlayer.Length - _mediaPlayer.Time;

            if (remainingMs > 5000)
                return;

            _autoplayTriggered = true;

            PlayNextEpisode();
        }

        private void PlayNextEpisode()
        {
            var currentSeasonIndex = _series.Seasons.FindIndex(
                s => s.Episodes.Contains(_episode));

            if (currentSeasonIndex == -1)
                return;

            var season = _series.Seasons[currentSeasonIndex];

            int currentEpisodeIndex = season.Episodes.FindIndex(
                e => e == _episode);

            Episode? nextEpisode = null;

            if (currentEpisodeIndex < season.Episodes.Count - 1)
            {
                nextEpisode = season.Episodes[currentEpisodeIndex + 1];
            }
            else if (currentSeasonIndex < _series.Seasons.Count - 1)
            {
                var nextSeason = _series.Seasons[currentSeasonIndex + 1];

                if (nextSeason.Episodes.Any())
                    nextEpisode = nextSeason.Episodes.First();
            }

            if (nextEpisode is null)
                return;

            CleanupPlayer();

            ((MainWindow)System.Windows.Application.Current.MainWindow)
                .MainContent.Content = new PlayerView(_series, nextEpisode);
        }

        private void ProgressSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSlider = true;
            ShowControls();
        }

        private void ProgressSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _mediaPlayer.Time = (long)ProgressSlider.Value;

            _isDraggingSlider = false;
            _autoplayTriggered = false;

            RestartResumeTimer();
            ShowControls();
        }

        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isDraggingSlider && _mediaPlayer.Length > 0)
            {
                TimeText.Text =
                    $"{TimeSpan.FromMilliseconds(ProgressSlider.Value):hh\\:mm\\:ss} / " +
                    $"{TimeSpan.FromMilliseconds(_mediaPlayer.Length):hh\\:mm\\:ss}";
            }
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePlayPause();
            ShowControls();
        }

        private void TogglePlayPause()
        {
            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Pause();
                PlayPauseButton.Content = "▶";
            }
            else
            {
                _mediaPlayer.Play();
                PlayPauseButton.Content = "⏸";
            }

            RestartResumeTimer();
        }

        private void SeekRelative(int seconds)
        {
            _autoplayTriggered = false;

            long newTime = Math.Clamp(
                _mediaPlayer.Time + (seconds * 1000L),
                0,
                _mediaPlayer.Length);

            _mediaPlayer.Time = newTime;

            RestartResumeTimer();
        }

        private void VolumeSlider_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mediaPlayer is null)
                return;

            int volume = (int)e.NewValue;

            _mediaPlayer.Volume = volume;

            VolumeIcon.Text = volume switch
            {
                0 => "🔇",
                < 50 => "🔉",
                _ => "🔊"
            };
        }

        private void VolumeSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _series.Volume = (int)VolumeSlider.Value;
            _storageService.SaveSeries(_series);

            ShowControls();
        }

        private async void MediaPlayer_Playing(object? sender, EventArgs e)
        {
            _mediaPlayer.Playing -= MediaPlayer_Playing;
            _autoplayTriggered = false;

            if (_startPosition.HasValue)
            {
                await Task.Delay(500);

                Dispatcher.Invoke(() =>
                {
                    _mediaPlayer.Time =
                        (long)_startPosition.Value.TotalMilliseconds;
                });
            }

            RestartResumeTimer();
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            CleanupPlayer();

            ((MainWindow)System.Windows.Application.Current.MainWindow)
                .MainContent.Content = new MenuView();
        }

        private void UpdateEpisodeInfo()
        {
            var season = _series.Seasons
                .FirstOrDefault(s => s.Episodes.Contains(_episode));

            if (season is null)
                return;

            EpisodeInfoText.Text =
                $"Season {season.Number}, Episode {_episode.Number}";
        }

        private void CleanupPlayer()
        {
            Mouse.OverrideCursor = null;

            _resumeTimer.Stop();
            _resumeTimer.Tick -= ResumeTimer_Tick;

            _uiTimer.Stop();
            _uiTimer.Tick -= UiTimer_Tick;

            _controlsHideTimer.Stop();
            _controlsHideTimer.Tick -= ControlsHideTimer_Tick;

            _mediaPlayer.Stop();

            VideoPlayer.MediaPlayer = null;

            _mediaPlayer.Dispose();
            _libVLC.Dispose();
        }
    }
}