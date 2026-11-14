using System;
using Windows.UI.Xaml.Media.Imaging;

namespace NeteaseGameBarWidget.Models
{
    /// <summary>
    /// Immutable-ish state passed from the SMTC service to the widget UI.
    /// </summary>
    public sealed class SmtcSnapshot
    {
        public bool HasSession { get; set; }

        public string Title { get; set; }

        public string Artist { get; set; }

        public string Album { get; set; }

        public string SourceApp { get; set; }

        public bool IsPlaying { get; set; }

        public bool CanPlay { get; set; }

        public bool CanPause { get; set; }

        public bool CanNext { get; set; }

        public bool CanPrevious { get; set; }

        public TimeSpan Position { get; set; }

        public TimeSpan Duration { get; set; }

        public BitmapImage Artwork { get; set; }

        public static SmtcSnapshot Empty()
        {
            return new SmtcSnapshot
            {
                HasSession = false,
                Title = "网易云音乐",
                Artist = "未检测到播放",
                Album = string.Empty,
                SourceApp = string.Empty,
                Position = TimeSpan.Zero,
                Duration = TimeSpan.Zero
            };
        }
    }
}
