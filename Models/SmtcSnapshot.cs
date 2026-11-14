using Windows.UI.Xaml.Media.Imaging;

namespace NeteaseGameBarWidget.Models
{
    /// <summary>
    /// What the widget needs from the current SMTC session: who is playing, what
    /// they are playing, what the transport allows, and the artwork.
    /// </summary>
    public sealed class SmtcSnapshot
    {
        public bool HasSession { get; set; }

        public string Title { get; set; }

        public string Artist { get; set; }

        public string Album { get; set; }

        public bool IsPlaying { get; set; }

        public bool CanPlay { get; set; }

        public bool CanPause { get; set; }

        public bool CanNext { get; set; }

        public bool CanPrevious { get; set; }

        public BitmapImage Artwork { get; set; }

        public static SmtcSnapshot Empty()
        {
            return new SmtcSnapshot
            {
                HasSession = false,
                Title = "网易云音乐",
                Artist = "未检测到播放",
                Album = string.Empty
            };
        }
    }
}
