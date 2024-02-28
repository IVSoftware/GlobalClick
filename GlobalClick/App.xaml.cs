
using WinRT.Interop;

namespace GlobalClick
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new AppShell();
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = base.CreateWindow(activationState);

#if WINDOWS
            window.HandlerChanged += (sender, args) =>
            {
                if (window.Handler?.PlatformView is Microsoft.UI.Xaml.Window winUIWindow)
                {
                    hMainWnd = WindowNative.GetWindowHandle(winUIWindow);
                }
            };
#endif
            return window;
        }

        public nint hMainWnd { get; private set; }
    }
}
