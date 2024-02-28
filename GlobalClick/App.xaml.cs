#if WINDOWS
using WinRT.Interop;
#endif

namespace GlobalClick
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new AppShell();
        }
    }
}
