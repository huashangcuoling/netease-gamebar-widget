using System;
using NeteaseGameBarWidget.Models;
using NeteaseGameBarWidget.Services;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;

namespace NeteaseGameBarWidget
{
    /// <summary>
    /// The widget card.
    ///
    /// The card keeps the original 377x65 metrics no matter what size the host
    /// gives it: type, cover and controls never scale, extra width simply gives
    /// the title more room before it ellipsises. It shows what SMTC actually
    /// publishes and nothing else: artwork, title, artist and album.
    /// </summary>
    public sealed partial class WidgetView : UserControl
    {
        private SmtcService _smtc;
        private BitmapImage _artwork;
        private string _trackKey = string.Empty;

        private bool _commandPending;
        private bool _unloaded;
        private bool _motion = true;

        public WidgetView()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += OnSizeChanged;

            try
            {
                _motion = new UISettings().AnimationsEnabled;
            }
            catch (Exception)
            {
                _motion = true;
            }
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            ApplyLayout();

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
            _unloaded = true;

            if (_smtc != null)
            {
                _smtc.SnapshotChanged -= OnSnapshotChanged;
                _smtc.Dispose();
                _smtc = null;
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyLayout();
        }

        /// <summary>
        /// The window height is locked to the taskbar height, so all this has to do
        /// is fit the card into that strip. Width is free: a wider strip only buys
        /// the title more room, it never changes a glyph size.
        /// </summary>
        private void ApplyLayout()
        {
            var height = ActualHeight;
            if (height <= 0)
            {
                return;
            }

            var roomy = height >= 44;
            var padX = roomy ? 10.0 : 8.0;
            var padY = roomy ? 6.0 : 4.0;

            Layout.Margin = new Thickness(padX, padY, padX, padY);

            var content = Math.Max(24, height - padY * 2);
            CoverHost.Width = content;
            CoverHost.Height = content;

            TitleText.FontSize = roomy ? 13 : 11.5;
            SubtitleText.FontSize = roomy ? 10.5 : 9.5;
            SubtitleText.Margin = new Thickness(0, roomy ? 1 : 0, 0, 0);

            TextColumn.Margin = new Thickness(padX, 0, padX, 0);

            var button = Math.Min(content, roomy ? 30 : 26);
            SetButtonMetrics(PreviousButton, button, button * 0.5);
            SetButtonMetrics(PlayPauseButton, Math.Min(content, roomy ? 34 : 28), button * 0.5);
            SetButtonMetrics(NextButton, button, button * 0.5);
        }

        private static void SetButtonMetrics(Button button, double size, double glyph)
        {
            button.Width = size;
            button.Height = size;
            button.FontSize = glyph;
        }

        private void OnSnapshotChanged(object sender, SmtcSnapshot snapshot)
        {
            if (_unloaded || snapshot == null)
            {
                return;
            }

            var key = snapshot.HasSession
                ? (snapshot.Title ?? string.Empty) + "\u001f" + (snapshot.Artist ?? string.Empty)
                : string.Empty;

            if (!string.Equals(key, _trackKey, StringComparison.Ordinal))
            {
                _trackKey = key;
                OnTrackChanged(snapshot);
            }
            else if (!ReferenceEquals(_artwork, snapshot.Artwork))
            {
                _artwork = snapshot.Artwork;
                ApplyArtwork(snapshot.Artwork, true);
            }

            UpdateTransport(snapshot);
        }

        private void UpdateTransport(SmtcSnapshot snapshot)
        {
            PlayPauseButton.Content = snapshot.IsPlaying ? "\uE769" : "\uE768";
            PreviousButton.IsEnabled = snapshot.CanPrevious;
            NextButton.IsEnabled = snapshot.CanNext;
            PlayPauseButton.IsEnabled = snapshot.IsPlaying ? snapshot.CanPause : snapshot.CanPlay;
        }

        private void OnTrackChanged(SmtcSnapshot snapshot)
        {
            TitleText.Text = string.IsNullOrWhiteSpace(snapshot.Title) ? "未知歌曲" : snapshot.Title;

            var subtitle = snapshot.Artist;
            if (!string.IsNullOrWhiteSpace(snapshot.Album))
            {
                subtitle = string.IsNullOrWhiteSpace(subtitle)
                    ? snapshot.Album
                    : subtitle + "  ·  " + snapshot.Album;
            }

            SubtitleText.Text = string.IsNullOrWhiteSpace(subtitle) ? "未知歌手" : subtitle;

            if (_motion)
            {
                Motion.FadeSlideFrom(TextColumn, TextTransform, 0, 8, 1, 0, Motion.Emphasized, 40, null);
            }
            else
            {
                TextColumn.Opacity = 1;
                TextTransform.TranslateY = 0;
            }

            _artwork = snapshot.Artwork;
            ApplyArtwork(snapshot.Artwork, true);
        }

        private void ApplyArtwork(BitmapImage artwork, bool animate)
        {
            if (artwork == null)
            {
                AlbumArtBorder.Visibility = Visibility.Collapsed;
                AlbumArtBorder.Opacity = 0;
                ArtworkPlaceholder.Visibility = Visibility.Visible;
                return;
            }

            AlbumArtBrush.ImageSource = artwork;
            ArtworkPlaceholder.Visibility = Visibility.Collapsed;
            AlbumArtBorder.Visibility = Visibility.Visible;

            if (!animate || !_motion)
            {
                AlbumArtBorder.Opacity = 1;
                CoverTransform.ScaleX = 1;
                CoverTransform.ScaleY = 1;
                return;
            }

            AlbumArtBorder.Opacity = 0;
            CoverTransform.ScaleX = 1.04;
            CoverTransform.ScaleY = 1.04;
            Motion.FadeTo(AlbumArtBorder, 1, Motion.Standard, 0, null);
            Motion.ScaleTo(CoverTransform, 1, Motion.Emphasized, 0, Motion.EaseOut(), null);
        }

        private void ShowSmtcError()
        {
            TitleText.Text = "无法读取网易云";
            SubtitleText.Text = "SMTC 初始化失败";
            AlbumArtBorder.Visibility = Visibility.Collapsed;
            ArtworkPlaceholder.Visibility = Visibility.Visible;
            PreviousButton.IsEnabled = false;
            PlayPauseButton.IsEnabled = false;
            NextButton.IsEnabled = false;
        }

        private void TransportButton_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null || !button.IsEnabled)
            {
                return;
            }

            SetButtonScale(button, 0.94, Motion.Quick, Motion.EaseOut());
        }

        private void TransportButton_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
            {
                return;
            }

            SetButtonScale(button, 1, Motion.Emphasized, Motion.Settle());
        }

        private void TransportButton_PointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
            {
                return;
            }

            SetButtonScale(button, 1, Motion.Quick, Motion.EaseOut());
        }

        private void SetButtonScale(Button button, double scale, int milliseconds, EasingFunctionBase easing)
        {
            var transform = button.RenderTransform as CompositeTransform;
            if (transform == null)
            {
                transform = new CompositeTransform();
                button.RenderTransform = transform;
                button.RenderTransformOrigin = new Point(0.5, 0.5);
            }

            if (!_motion)
            {
                transform.ScaleX = scale;
                transform.ScaleY = scale;
                return;
            }

            Motion.ScaleTo(transform, scale, milliseconds, 0, easing, null);
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
    }
}
