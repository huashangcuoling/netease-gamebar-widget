using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace NeteaseGameBarWidget.Services
{
    /// <summary>
    /// Storyboard helpers. Every animation in the widget goes through this class
    /// so timing and easing stay consistent, and so a reduced-motion preference
    /// can bypass motion entirely.
    /// </summary>
    internal static class Motion
    {
        /// <summary>A pointer press.</summary>
        public const int Quick = 110;

        /// <summary>A content crossfade.</summary>
        public const int Standard = 190;

        /// <summary>A track change.</summary>
        public const int Emphasized = 260;

        public static EasingFunctionBase EaseOut()
        {
            return new CubicEase { EasingMode = EasingMode.EaseOut };
        }

        public static EasingFunctionBase Settle()
        {
            return new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.35 };
        }

        public static void FadeTo(UIElement element, double opacity, int milliseconds, int delayMilliseconds, Action completed)
        {
            if (element == null)
            {
                return;
            }

            var storyboard = new Storyboard();
            var animation = new DoubleAnimation
            {
                To = opacity,
                Duration = new Duration(TimeSpan.FromMilliseconds(milliseconds)),
                BeginTime = TimeSpan.FromMilliseconds(delayMilliseconds),
                EasingFunction = EaseOut()
            };

            Storyboard.SetTarget(animation, element);
            Storyboard.SetTargetProperty(animation, "Opacity");
            storyboard.Children.Add(animation);
            Run(storyboard, completed);
        }

        public static void ScaleTo(CompositeTransform transform, double scale, int milliseconds, int delayMilliseconds, EasingFunctionBase easing, Action completed)
        {
            if (transform == null)
            {
                return;
            }

            var storyboard = new Storyboard();
            var duration = new Duration(TimeSpan.FromMilliseconds(milliseconds));
            var begin = TimeSpan.FromMilliseconds(delayMilliseconds);

            var scaleX = new DoubleAnimation { To = scale, Duration = duration, BeginTime = begin, EasingFunction = easing ?? EaseOut() };
            Storyboard.SetTarget(scaleX, transform);
            Storyboard.SetTargetProperty(scaleX, "ScaleX");
            storyboard.Children.Add(scaleX);

            var scaleY = new DoubleAnimation { To = scale, Duration = duration, BeginTime = begin, EasingFunction = easing ?? EaseOut() };
            Storyboard.SetTarget(scaleY, transform);
            Storyboard.SetTargetProperty(scaleY, "ScaleY");
            storyboard.Children.Add(scaleY);

            Run(storyboard, completed);
        }

        /// <summary>
        /// Animates from explicit start values instead of the current ones, so a
        /// previous storyboard that is still holding a property cannot skew it.
        /// </summary>
        public static void FadeSlideFrom(UIElement element, CompositeTransform transform, double fromOpacity, double fromY, double toOpacity, double toY, int milliseconds, int delayMilliseconds, Action completed)
        {
            if (element == null)
            {
                return;
            }

            var storyboard = new Storyboard();
            var duration = new Duration(TimeSpan.FromMilliseconds(milliseconds));
            var begin = TimeSpan.FromMilliseconds(delayMilliseconds);

            var fade = new DoubleAnimation { From = fromOpacity, To = toOpacity, Duration = duration, BeginTime = begin, EasingFunction = EaseOut() };
            Storyboard.SetTarget(fade, element);
            Storyboard.SetTargetProperty(fade, "Opacity");
            storyboard.Children.Add(fade);

            if (transform != null)
            {
                var slide = new DoubleAnimation { From = fromY, To = toY, Duration = duration, BeginTime = begin, EasingFunction = EaseOut() };
                Storyboard.SetTarget(slide, transform);
                Storyboard.SetTargetProperty(slide, "TranslateY");
                storyboard.Children.Add(slide);
            }

            Run(storyboard, completed);
        }

        private static void Run(Storyboard storyboard, Action completed)
        {
            if (completed != null)
            {
                storyboard.Completed += (sender, args) => completed();
            }

            storyboard.Begin();
        }
    }
}
