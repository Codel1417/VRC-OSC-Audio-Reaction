using NAudio.CoreAudioApi;
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

        public Audio audio = new();
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "Runs in background")]
        private readonly WindowColor windowColor = new();
        public MainWindow()
        {
            Helpers.InitLogging(false);
            InitializeComponent();
        }

        private async void Window_Initialized(object sender, EventArgs e)
        {
            audio.mainWindow = this;
            Logger.Info($"Version: {Helpers.AssemblyProductVersion}");
            //Load settings
            if (Settings.Default.error_reporting) await Helpers.InitSentry();
            await Helpers.CheckGitHubNewerVersion().ConfigureAwait(false); // Don't wait for it
            await audio.SetUpAudio().ConfigureAwait(false);
            await audio.Update().ConfigureAwait(false);
            return;
        }

        private void Window_Closed(object sender, EventArgs e) => Environment.Exit(0);


        private void Save_config_Click(object sender, RoutedEventArgs e) => Settings.Default.Save();



        private void Osc_port_input_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Reset_config_Click(object sender, RoutedEventArgs e) => Settings.Default.Reset();
    }
}