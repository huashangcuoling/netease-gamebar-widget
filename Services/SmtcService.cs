using System;
using System.Threading;
using System.Threading.Tasks;
using NeteaseGameBarWidget.Models;
using Windows.Media.Control;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;

namespace NeteaseGameBarWidget.Services
{
    /// <summary>
    /// Reads the system-wide SMTC session published by NetEase Cloud Music.
    /// Requires the restricted globalMediaControl capability in Package.appxmanifest.
    /// </summary>
    public sealed class SmtcService : IDisposable
    {
        private readonly CoreDispatcher _dispatcher;
        private readonly SemaphoreSlim _refreshGate = new SemaphoreSlim(1, 1);

        private GlobalSystemMediaTransportControlsSessionManager _manager;
        private GlobalSystemMediaTransportControlsSession _session;
        private BitmapImage _artwork;
        private string _artworkKey;
        private bool _disposed;

        public SmtcService(CoreDispatcher dispatcher)
        {
            if (dispatcher == null)
            {
                throw new ArgumentNullException(nameof(dispatcher));
            }

            _dispatcher = dispatcher;
        }

        public event EventHandler<SmtcSnapshot> SnapshotChanged;

        public async Task InitializeAsync()
        {
            if (_disposed)
            {
                return;
            }

            var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (_disposed)
            {
                return;
            }

            _manager = manager;
            _manager.CurrentSessionChanged += OnCurrentSessionChanged;
            _manager.SessionsChanged += OnSessionsChanged;

            AttachSession(FindNeteaseSession());
            await RefreshAsync();
        }

        public async Task<bool> TogglePlayPauseAsync()
        {
            var session = _session;
            if (session == null)
            {
                return false;
            }

            try
            {
                return await session.TryTogglePlayPauseAsync();
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SkipNextAsync()
        {
            var session = _session;
            if (session == null)
            {
                return false;
            }

            try
            {
                return await session.TrySkipNextAsync();
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SkipPreviousAsync()
        {
            var session = _session;
            if (session == null)
            {
                return false;
            }

            try
            {
                return await session.TrySkipPreviousAsync();
            }
            catch
            {
                return false;
            }
        }

        private void OnCurrentSessionChanged(
            GlobalSystemMediaTransportControlsSessionManager sender,
            CurrentSessionChangedEventArgs args)
        {
            QueueRefresh();
        }

        private void OnSessionsChanged(
            GlobalSystemMediaTransportControlsSessionManager sender,
            SessionsChangedEventArgs args)
        {
            QueueRefresh();
        }

        private void OnMediaPropertiesChanged(
            GlobalSystemMediaTransportControlsSession sender,
            MediaPropertiesChangedEventArgs args)
        {
            QueueRefresh();
        }

        private void OnPlaybackInfoChanged(
            GlobalSystemMediaTransportControlsSession sender,
            PlaybackInfoChangedEventArgs args)
        {
            QueueRefresh();
        }

        private void OnTimelinePropertiesChanged(
            GlobalSystemMediaTransportControlsSession sender,
            TimelinePropertiesChangedEventArgs args)
        {
            QueueRefresh();
        }

        private void QueueRefresh()
        {
            if (_disposed)
            {
                return;
            }

            var ignored = _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                try
                {
                    if (_disposed)
                    {
                        return;
                    }

                    AttachSession(FindNeteaseSession());
                    await RefreshAsync();
                }
                catch
                {
                    // A session can disappear between enumeration and read.
                }
            });
        }

        private void AttachSession(GlobalSystemMediaTransportControlsSession session)
        {
            if (ReferenceEquals(_session, session))
            {
                return;
            }

            DetachSession();
            _session = session;

            if (_session != null)
            {
                _session.MediaPropertiesChanged += OnMediaPropertiesChanged;
                _session.PlaybackInfoChanged += OnPlaybackInfoChanged;
                _session.TimelinePropertiesChanged += OnTimelinePropertiesChanged;
            }
        }

        private void DetachSession()
        {
            if (_session == null)
            {
                return;
            }

            try
            {
                _session.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                _session.PlaybackInfoChanged -= OnPlaybackInfoChanged;
                _session.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
            }
            catch
            {
                // The session may already be closed.
            }

            _session = null;
        }

        private GlobalSystemMediaTransportControlsSession FindNeteaseSession()
        {
            if (_manager == null)
            {
                return null;
            }

            GlobalSystemMediaTransportControlsSession current = null;
            try
            {
                current = _manager.GetCurrentSession();
            }
            catch
            {
            }

            if (IsNetease(current))
            {
                return current;
            }

            GlobalSystemMediaTransportControlsSession fallback = null;
            try
            {
                foreach (var session in _manager.GetSessions())
                {
                    if (!IsNetease(session))
                    {
                        continue;
                    }

                    if (fallback == null)
                    {
                        fallback = session;
                    }

                    try
                    {
                        if (session.GetPlaybackInfo().PlaybackStatus ==
                            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                        {
                            return session;
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }

            return fallback;
        }

        private static bool IsNetease(GlobalSystemMediaTransportControlsSession session)
        {
            if (session == null)
            {
                return false;
            }

            var source = session.SourceAppUserModelId;
            return !string.IsNullOrEmpty(source) &&
                   source.IndexOf("cloudmusic", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private async Task RefreshAsync()
        {
            await _refreshGate.WaitAsync();
            try
            {
                if (_disposed)
                {
                    return;
                }

                var session = _session;
                if (session == null)
                {
                    PublishEmpty();
                    return;
                }

                GlobalSystemMediaTransportControlsSessionMediaProperties properties = null;
                GlobalSystemMediaTransportControlsSessionPlaybackInfo playback = null;
                GlobalSystemMediaTransportControlsSessionTimelineProperties timeline = null;

                try
                {
                    properties = await session.TryGetMediaPropertiesAsync();
                }
                catch
                {
                }

                try
                {
                    playback = session.GetPlaybackInfo();
                }
                catch
                {
                }

                try
                {
                    timeline = session.GetTimelineProperties();
                }
                catch
                {
                }

                if (properties == null)
                {
                    PublishEmpty();
                    return;
                }

                var artworkKey = (properties.Title ?? string.Empty) + "\u001f" +
                                 (properties.Artist ?? string.Empty) + "\u001f" +
                                 (properties.AlbumTitle ?? string.Empty) + "\u001f" +
                                 (properties.Thumbnail == null ? "0" : "1");

                if (!string.Equals(artworkKey, _artworkKey, StringComparison.Ordinal))
                {
                    _artworkKey = artworkKey;
                    _artwork = await LoadArtworkAsync(properties.Thumbnail);
                }

                var start = timeline != null ? timeline.StartTime : TimeSpan.Zero;
                var end = timeline != null ? timeline.EndTime : TimeSpan.Zero;
                var position = timeline != null ? timeline.Position : TimeSpan.Zero;
                var duration = end > start ? end - start : TimeSpan.Zero;
                var elapsed = position > start ? position - start : TimeSpan.Zero;

                if (elapsed < TimeSpan.Zero)
                {
                    elapsed = TimeSpan.Zero;
                }

                if (duration > TimeSpan.Zero && elapsed > duration)
                {
                    elapsed = duration;
                }

                var controls = playback != null ? playback.Controls : null;

                var snapshot = new SmtcSnapshot
                {
                    HasSession = true,
                    Title = string.IsNullOrWhiteSpace(properties.Title) ? "未知歌曲" : properties.Title,
                    Artist = string.IsNullOrWhiteSpace(properties.Artist) ? "未知歌手" : properties.Artist,
                    Album = properties.AlbumTitle ?? string.Empty,
                    SourceApp = session.SourceAppUserModelId ?? string.Empty,
                    IsPlaying = playback != null && playback.PlaybackStatus ==
                        GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
                    CanPlay = controls != null && controls.IsPlayEnabled,
                    CanPause = controls != null && controls.IsPauseEnabled,
                    CanNext = controls != null && controls.IsNextEnabled,
                    CanPrevious = controls != null && controls.IsPreviousEnabled,
                    Position = elapsed,
                    Duration = duration,
                    Artwork = _artwork
                };

                SnapshotChanged?.Invoke(this, snapshot);
            }
            finally
            {
                _refreshGate.Release();
            }
        }

        private void PublishEmpty()
        {
            _artwork = null;
            _artworkKey = null;
            SnapshotChanged?.Invoke(this, SmtcSnapshot.Empty());
        }

        private static async Task<BitmapImage> LoadArtworkAsync(IRandomAccessStreamReference reference)
        {
            if (reference == null)
            {
                return null;
            }

            try
            {
                using (var stream = await reference.OpenReadAsync())
                {
                    var image = new BitmapImage();
                    await image.SetSourceAsync(stream);
                    return image;
                }
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_manager != null)
            {
                try
                {
                    _manager.CurrentSessionChanged -= OnCurrentSessionChanged;
                    _manager.SessionsChanged -= OnSessionsChanged;
                }
                catch
                {
                }
            }

            DetachSession();
            _manager = null;
            _artwork = null;
            _artworkKey = null;
        }
    }
}
