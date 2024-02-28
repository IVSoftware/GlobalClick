## Global Click

For a windows-only solution you could experiment with using P/Invoke to install a low-level mouse hook. Here's my tested solution, which demonstrates this by showing the image for 2 seconds, and restarting the interval on every click anywhere on the page. The hook itself is going to detect a click anywhere on the desktop, and this requires capturing the native handle for `MainPage` so that we can compare it's current position to that of the mouse click.

[![screenshot][1]][1]
```csharp
    public partial class MainPage : ContentPage
    {
        int count = 0;
        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
            ClickedAnywhere += (sender, e) =>
            {
                WatchdogTimer.StartOrRestart(
                    initialAction: () => TimerRunning = true,
                    completeAction: () => TimerRunning = false
                );
            };
        }
        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }
        WatchdogTimer WatchdogTimer { get; } = new WatchdogTimer { Interval = TimeSpan.FromSeconds(2.5) };
        public bool TimerRunning
        {
            get => _timerRunning;
            set
            {
                if (!Equals(_timerRunning, value))
                {
                    _timerRunning = value;
                    OnPropertyChanged();
                }
            }
        }
        bool _timerRunning = false;

        protected override void OnAppearing()
        {
            base.OnAppearing();
            SetHook();
        }
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            ReleaseHook();
        }
        IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                switch (wParam) 
                {
                    case WM_LBUTTONDOWN:
                        var info = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                        Debug.WriteLine($"X: {info.pt.x} Y: {info.pt.y}");
                        RECT rect = new();
                        var hWnd = ((App)App.Current).hMainWnd;
                        if(GetWindowRect(hWnd, out rect))
                        {
                            Debug.WriteLine($"Contained {localIsPointInRect()} Left: {rect.Left} Right: {rect.Right} Top: {rect.Top} Bottom: {rect.Bottom}");

                            if (localIsPointInRect())
                            {
                                Dispatcher.Dispatch(() => ClickedAnywhere?.Invoke(
                                    hWnd,
                                    EventArgs.Empty)
                                );
                            }

                            bool localIsPointInRect()
                            {
                                if (info.pt.x < rect.Left) return false;
                                if (info.pt.x > rect.Right) return false;
                                if (info.pt.y < rect.Top) return false;
                                if (info.pt.y > rect.Bottom) return false;
                                return true;
                            }
                        }
                        break;
                }
            }
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }
        event EventHandler ClickedAnywhere;

        #region P / I N V O K E

        const int WM_LBUTTONDOWN = 0x0201; 
        
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        void SetHook()
        {
            _proc = LowLevelMouseProc;
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _hookID = SetWindowsHookEx(WH_MOUSE_LL, _proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }
        private static IntPtr _hookID = IntPtr.Zero;
        private static LowLevelMouseProc _proc; 

        void ReleaseHook()
        {
            UnhookWindowsHookEx(_hookID);
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        public const int WH_MOUSE_LL = 14;
        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT Point); 
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        #endregion P / I N V O K E
    }
    public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
```
___

```xaml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="GlobalClick.MainPage">

    <ScrollView>
        <VerticalStackLayout
            Spacing="25"
            Padding="30,0"
            VerticalOptions="Center">

            <Image
                Source="dotnet_bot.png"
                SemanticProperties.Description="Cute dot net bot waving hi to you!"
                HeightRequest="200"
                HorizontalOptions="Center" 
                IsVisible="{Binding TimerRunning}"/>

            <Label
                Text="Hello, World!"
                SemanticProperties.HeadingLevel="Level1"
                FontSize="32"
                HorizontalOptions="Center" />

            <Button
                x:Name="CounterBtn"
                Text="Click me"
                SemanticProperties.Hint="Counts the number of times you click"
                Clicked="OnCounterClicked"
                HorizontalOptions="Center" />

        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

##### Capture the main window handle.

```csharp
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
```

  [1]: https://i.stack.imgur.com/vUmO1.png