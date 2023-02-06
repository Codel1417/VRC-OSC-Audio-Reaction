using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using VRC_OSC_AudioEars.Audio;
using VRC_OSC_AudioEars.Properties;

namespace VRC_OSC_AudioEars
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "Runs in background")]
        private readonly WindowColor windowColor = new();
        public MainWindow()
        {
            Helpers.MainWindow = this;
            InitializeComponent();
            AudioReactionControl.Start();
            AudioReactionControl.SetUpAudio();
        }

        private async void Window_Initialized(object sender, EventArgs e)
        {
            //Load settings
            await Helpers.CheckGitHubNewerVersion().ConfigureAwait(false); // Don't wait for it
        }

        private void Window_Closed(object sender, EventArgs e) => Environment.Exit(0);


        private void Save_config_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Save();
            if (SnackBar is { MessageQueue: { } })
            {
                SnackBar.MessageQueue.Enqueue("Settings Saved!");
            }
        }

        private void Osc_port_input_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Reset_config_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Reset();
            if (SnackBar is { MessageQueue: { } })
            {
                SnackBar.MessageQueue.Enqueue("Settings Reset!");
            }
        }

        private void settingsButton_Click(object sender, RoutedEventArgs e) => Transitioner.SelectedIndex = 1;

        private void HomeButton_Click(object sender, RoutedEventArgs e) => Transitioner.SelectedIndex = 0;

        private void aboutButton_Click(object sender, RoutedEventArgs e) => Transitioner.SelectedIndex = 2;

        private void CheckBox_Checked(object sender, RoutedEventArgs e) => AudioReactionControl.Enable();

        private async void GithubButtonClick(object sender, RoutedEventArgs e) => await Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/Codel1417/VRC-OSC-Audio-Reaction"));

        private void ToggleButton_OnUnchecked(object sender, RoutedEventArgs e) => AudioReactionControl.Disable();

        private void RangeBase_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => AudioReactionControl.UpdateGain((float) e.NewValue);
    }
}