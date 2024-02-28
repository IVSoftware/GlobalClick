using IVSoftware.Portable;
using System.Diagnostics;
using System.Runtime.InteropServices;
#if WINDOWS
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
#endif

namespace GlobalClick
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        int _debugCount = 0;
        List<ContentView>_children = new List<ContentView>();
        public MainPage()
        {
            InitializeComponent();

            foreach (var desc in this.GetVisualTreeDescendants().OfType<View>())
            {
                var tapGestureRecognizer = new TapGestureRecognizer();
                tapGestureRecognizer.Tapped += (sender, e) =>
                {
                    WatchdogTimer.StartOrRestart(
                        initialAction: () => TimerRunning = true,
                        completeAction: () => TimerRunning = false
                    );
                };
                desc.GestureRecognizers.Add(tapGestureRecognizer);
            }
            BindingContext = this;
            ClickedAnywhere += (sender, e) =>
            {
                //WatchdogTimer.StartOrRestart(
                //    initialAction: () => TimerRunning = true,
                //    completeAction: () => TimerRunning = false
                //);
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
}
