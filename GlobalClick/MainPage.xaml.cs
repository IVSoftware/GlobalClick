using IVSoftware.Portable;

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
                if (desc is Button button)
                {
                    button.Clicked += localOnAnyInput;
                }
                else if (desc is Entry entry)
                {
                    entry.TextChanged += localOnAnyInput;
                    entry.Focused += localOnAnyInput;
                }
                else
                {
                    var tapGestureRecognizer = new TapGestureRecognizer();
                    tapGestureRecognizer.Tapped += localOnAnyInput;
                    desc.GestureRecognizers.Add(tapGestureRecognizer);
                }

                void localOnAnyInput(object sender, EventArgs e)
                {
                    WatchdogTimer.StartOrRestart(
                        initialAction: () => TimerRunning = true,
                        completeAction: () => TimerRunning = false
                    );
                }
            }
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
    }
}
