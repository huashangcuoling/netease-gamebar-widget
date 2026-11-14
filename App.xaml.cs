using System;
using Microsoft.Gaming.XboxGameBar;
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

                        // Keep the compact card at exactly 377 x 65 effective pixels.
                        // The manifest also declares this size, so Game Bar uses it
                        // before the XAML page has loaded.
                        _widget.MinWindowSize = new Size(377, 65);
                        _widget.MaxWindowSize = new Size(377, 65);
                        _widget.HorizontalResizeSupported = false;
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
