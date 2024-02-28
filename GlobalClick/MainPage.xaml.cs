using IVSoftware.Portable;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GlobalClick
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
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
                        WatchdogTimer.StartOrRestart(
                            initialAction: () => TimerRunning = true,
                            completeAction: () => TimerRunning = false
                        );
                        break;
                }
            }
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }
        #region P / I N V O K E

        const int WM_LBUTTONDOWN = 0x0201;
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
        #endregion P / I N V O K E
    }
    public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
}
