using System;
using Microsoft.Gaming.XboxGameBar;
using NeteaseGameBarWidget.Services;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace NeteaseGameBarWidget
{
    sealed partial class App : Application
    {
        private XboxGameBarWidget _widget;

        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            if (args.Kind == ActivationKind.Protocol)
            {
                var protocolArgs = args as IProtocolActivatedEventArgs;
                if (protocolArgs != null &&
                    protocolArgs.Uri.Scheme.Equals("ms-gamebarwidget", StringComparison.OrdinalIgnoreCase))
                {
                    var widgetArgs = args as XboxGameBarWidgetActivatedEventArgs;
                    if (widgetArgs != null && widgetArgs.IsLaunchActivation)
                    {
                        var rootFrame = new Frame();
                        rootFrame.NavigationFailed += OnNavigationFailed;
                        Window.Current.Content = rootFrame;

                        _widget = new XboxGameBarWidget(
                            widgetArgs,
                            Window.Current.CoreWindow,
                            rootFrame);

                        // The card is a strip that sits flush with the taskbar: the
                        // height is locked to the taskbar height in DIPs, the width
                        // is free so it can span whatever the user wants.
                        // settings.json can override the guess when the taskbar is
                        // non-standard (small buttons, custom shell).
                        var taskbar = WidgetSettings.TaskbarHeight() ?? GetTaskbarHeight();
                        _widget.MinWindowSize = new Size(300, taskbar);
                        _widget.MaxWindowSize = new Size(600, taskbar);
                        _widget.HorizontalResizeSupported = true;
                        _widget.VerticalResizeSupported = false;
                        _widget.PinningSupported = true;
                        _widget.HomeMenuVisibleSupported = true;

                        rootFrame.Navigate(typeof(WidgetPage));
                        Window.Current.Closed += OnWidgetWindowClosed;
                        Window.Current.Activate();
                        return;
                    }
                }
            }

            base.OnActivated(args);
        }

        /// <summary>
        /// Taskbar height in effective pixels. Windows 11 draws a 48 DIP taskbar and
        /// Windows 10 a 40 DIP one; both scale with the display DPI, so DIPs are the
        /// right unit for the widget window.
        /// </summary>
        private static double GetTaskbarHeight()
        {
            try
            {
                var raw = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
                ulong version;
                if (ulong.TryParse(raw, out version))
                {
                    var build = (version >> 16) & 0xFFFF;
                    return build >= 22000 ? 48 : 40;
                }
            }
            catch (Exception)
            {
            }

            return 48;
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            var rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                Window.Current.Content = rootFrame;
            }

            if (!e.PrelaunchActivated && rootFrame.Content == null)
            {
                rootFrame.Navigate(typeof(MainPage), e.Arguments);
            }

            Window.Current.Activate();
        }

        private void OnWidgetWindowClosed(object sender, CoreWindowEventArgs args)
        {
            Window.Current.Closed -= OnWidgetWindowClosed;
            _widget = null;
        }

        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load page: " + e.SourcePageType.FullName);
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            _widget = null;
            deferral.Complete();
        }
    }
}
