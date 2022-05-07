using NLog;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace VRC_OSC_AudioEars
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public Audio audio = Audio.Instance;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "Runs in background")]
        private readonly WindowColor windowColor = new();
        public String WindowTitle = "VRC OSC Audio Ears" + Helpers.AssemblyProductVersion;
        public MainWindow()
        {
            Helpers.mainWindow = this;
            Helpers.InitLogging(false);
            InitializeComponent();
        }

        private async void Window_Initialized(object sender, EventArgs e)
        {
            Logger.Info($"Version: {Helpers.AssemblyProductVersion}");
            //Load settings
            if (Settings.Default.error_reporting) await Helpers.InitSentry();
            await Helpers.CheckGitHubNewerVersion().ConfigureAwait(false); // Don't wait for it
            await audio.Update().ConfigureAwait(false);// main update loop
            this.Title = $" - {Helpers.AssemblyProductVersion}";
        }

        private void Window_Closed(object sender, EventArgs e) => Environment.Exit(0);


        private void Save_config_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Save();
            if (SnackBar != null && SnackBar.MessageQueue != null)
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
            if (SnackBar != null && SnackBar.MessageQueue != null)
            {
                SnackBar.MessageQueue.Enqueue("Settings Reset!");
            }
        }

        private void settingsButton_Click(object sender, RoutedEventArgs e)
        {
            Transitioner.SelectedIndex = 1;
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            Transitioner.SelectedIndex = 0;
        }

        private void aboutButton_Click(object sender, RoutedEventArgs e)
        {
            Transitioner.SelectedIndex = 2;
        }

        private void DeviceName_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs? e)
        {
            if (DeviceName != null && DeviceName.SelectedItem != null)
            {
                audio.SetUpAudio((string)DeviceName.SelectedItem, DeviceName);
            }
        }

        private void DeviceName_Initialized(object sender, EventArgs e)
        {
            DeviceName = audio.UpdateUIDeviceList(DeviceName);
            DeviceName.InvalidateVisual();
            DeviceName.UpdateLayout();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            DeviceName_SelectionChanged(sender, null);
        }
    }
}