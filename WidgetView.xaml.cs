using System;
using NeteaseGameBarWidget.Models;
using NeteaseGameBarWidget.Services;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace NeteaseGameBarWidget
{
    public sealed partial class WidgetView : UserControl
    {
        private readonly DispatcherTimer _progressTimer;
        private SmtcService _smtc;
        private double _progressRatio;
        private bool _commandPending;
        private bool _isPlaying;
        private TimeSpan _position;
        private TimeSpan _duration;
        private DateTimeOffset _timelineUpdatedAt;
        private readonly SolidColorBrush _normalGlyphBrush =
            new SolidColorBrush(Color.FromArgb(0xF2, 0xFF, 0xFF, 0xFF));
        private readonly SolidColorBrush _hoverGlyphBrush =
            new SolidColorBrush(Color.FromArgb(0xFF, 0x8F, 0xD8, 0xFF));

        public WidgetView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            ProgressTrack.SizeChanged += OnProgressTrackSizeChanged;

            _progressTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _progressTimer.Tick += OnProgressTimerTick;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_smtc != null)
            {
                return;
            }

            try
            {
                var dispatcher = Window.Current != null && Window.Current.CoreWindow != null
                    ? Window.Current.CoreWindow.Dispatcher
                    : CoreApplication.MainView.CoreWindow.Dispatcher;

                _smtc = new SmtcService(dispatcher);
                _smtc.SnapshotChanged += OnSnapshotChanged;
                await _smtc.InitializeAsync();
            }
            catch (Exception)
            {
                ShowSmtcError();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _progressTimer.Stop();

            if (_smtc == null)
            {
                return;
            }

            _smtc.SnapshotChanged -= OnSnapshotChanged;
            _smtc.Dispose();
            _smtc = null;
        }

        private void OnSnapshotChanged(object sender, SmtcSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            TitleText.Text = snapshot.Title;
            ArtistText.Text = snapshot.Artist;
            AlbumArtBrush.ImageSource = snapshot.Artwork;
            AlbumArtBorder.Visibility = snapshot.Artwork != null ? Visibility.Visible : Visibility.Collapsed;
            ArtworkPlaceholder.Visibility = snapshot.Artwork != null ? Visibility.Collapsed : Visibility.Visible;

            PlayPauseButton.Content = snapshot.IsPlaying ? "\uE769" : "\uE768";
            PreviousButton.IsEnabled = snapshot.CanPrevious;
            NextButton.IsEnabled = snapshot.CanNext;
            PlayPauseButton.IsEnabled = snapshot.IsPlaying ? snapshot.CanPause : snapshot.CanPlay;

            _isPlaying = snapshot.IsPlaying;
            _position = snapshot.Position;
            _duration = snapshot.Duration;
            _timelineUpdatedAt = DateTimeOffset.Now;
            _progressRatio = _duration.TotalMilliseconds > 0
                ? _position.TotalMilliseconds / _duration.TotalMilliseconds
                : 0;

            ProgressTrack.Visibility = snapshot.HasSession && _duration > TimeSpan.Zero
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (_isPlaying && _duration > TimeSpan.Zero)
            {
                _progressTimer.Start();
            }
            else
            {
                _progressTimer.Stop();
            }

            UpdateProgressWidth();
        }

        private void OnProgressTrackSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateProgressWidth();
        }

        private void OnProgressTimerTick(object sender, object e)
        {
            if (!_isPlaying || _duration <= TimeSpan.Zero)
            {
                return;
            }

            var elapsed = _position + (DateTimeOffset.Now - _timelineUpdatedAt);
            _progressRatio = elapsed.TotalMilliseconds / _duration.TotalMilliseconds;
            UpdateProgressWidth();
        }

        private void UpdateProgressWidth()
        {
            var width = ProgressTrack.ActualWidth;
            if (width <= 0 || _progressRatio <= 0)
            {
                ProgressFill.Width = 0;
                return;
            }

            var ratio = Math.Max(0, Math.Min(1, _progressRatio));
            ProgressFill.Width = Math.Max(1, width * ratio);
        }

        private void TransportButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.IsEnabled)
            {
                button.Foreground = _hoverGlyphBrush;
            }
        }

        private void TransportButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                button.Foreground = _normalGlyphBrush;
            }
        }

        private async void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_smtc == null || _commandPending)
            {
                return;
            }

            _commandPending = true;
            try
            {
                await _smtc.TogglePlayPauseAsync();
            }
            finally
            {
                _commandPending = false;
            }
        }

        private async void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (_smtc == null || _commandPending)
            {
                return;
            }

            _commandPending = true;
            try
            {
                await _smtc.SkipPreviousAsync();
            }
            finally
            {
                _commandPending = false;
            }
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_smtc == null || _commandPending)
            {
                return;
            }

            _commandPending = true;
            try
            {
                await _smtc.SkipNextAsync();
            }
            finally
            {
                _commandPending = false;
            }
        }

        private void ShowSmtcError()
        {
            TitleText.Text = "无法读取网易云";
            ArtistText.Text = "SMTC 初始化失败";
            AlbumArtBorder.Visibility = Visibility.Collapsed;
            ArtworkPlaceholder.Visibility = Visibility.Visible;
            PreviousButton.IsEnabled = false;
            PlayPauseButton.IsEnabled = false;
            NextButton.IsEnabled = false;
            ProgressTrack.Visibility = Visibility.Collapsed;
            _progressTimer.Stop();
        }
    }
}
