using IVSoftware.Portable;
using System.Diagnostics;

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
            TapGestureRecognizer tapGestureRecognizer;
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
                    entry.Completed += (sender, e) =>
                    {
                    };
                }
                else
                {
                    tapGestureRecognizer = new TapGestureRecognizer();
                    tapGestureRecognizer.Tapped += localOnAnyInput;
                    desc.GestureRecognizers.Add(tapGestureRecognizer);
                }

                void localOnAnyInput(object sender, EventArgs e)
                {
                    // Close soft input if sender isn't an Entry control.
                    if (!(sender is Entry))
                    {
                        localCloseSoftInput();
                    }

                    localStartOrRestartWDT();

                    void localStartOrRestartWDT()
                    {
                        WatchdogTimer.StartOrRestart(
                            initialAction: () =>
                            {
                                TimerRunning = true;
                            },
                            completeAction: () =>
                            {
                                if (this.GetVisualTreeDescendants().OfType<Entry>().Any(_ => _.IsFocused))
                                {
                                    // Do not pull the rug out when user is editing an entry.
                                    localStartOrRestartWDT();
                                }
                                else
                                {
                                    TimerRunning = false;
                                }
                            });
                    }
                }
                void localCloseSoftInput()
                {
                    foreach (var entry in this.GetVisualTreeDescendants().OfType<Entry>())
                    {
                        entry.IsEnabled = false;
                        entry.IsEnabled = true;
                    }
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
