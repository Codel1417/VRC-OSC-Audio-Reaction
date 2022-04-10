using BuildSoft.VRChat.Osc;
using CommandLine;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VRC_OSC_AudioEars
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        Audio audio = new();

        public MainWindow()
        {
            Helpers.InitLogging(false);
            InitializeComponent();
        }

        public static bool Verbose { get; set; }
        public static bool DisableSentry { get; set; }

        private void OnGainSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => Properties.gain = (float)e.NewValue;



        private void OnEnableApp(object sender, RoutedEventArgs e) => Properties.enabled = true;

        private void OnDisableApp(object sender, RoutedEventArgs e) => Properties.enabled = false;

        private async void Window_Initialized(object sender, EventArgs e)
        {
            audio.mainWindow = this;
            Logger.Info($"Version: {Helpers.AssemblyProductVersion}");
            string[] args = Environment.GetCommandLineArgs();
            Parser.Default.ParseArguments<CLI_Options>(args)
                .WithParsed(options => RunOptions(options))
                .WithNotParsed(errors => HandleParseError(errors));
            //Load settings
            if (Properties.sentry_reporting_enabled) await Helpers.InitSentry();
            await Helpers.CheckGitHubNewerVersion().ConfigureAwait(false); // Don't wait for it
            await audio.SetUpAudio().ConfigureAwait(false);
            await audio.Update().ConfigureAwait(false);
            return;
        }
        private static void HandleParseError(IEnumerable<Error> obj)
        {
            MessageBox.Show(obj.ToString());
            Environment.Exit(1);
        }

        private static void RunOptions(CLI_Options obj)
        {
            if (obj.Verbose)
            {
                Verbose = true;
            }
            if (obj.DisableSentry)
            {
                Properties.sentry_reporting_enabled = false;
            }
        }

        private void Window_Closed(object sender, EventArgs e) => Environment.Exit(0);

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void osc_vol_par_input_Initialized(object sender, EventArgs e) => osc_vol_par_input.Text = Properties.audio_volume_parameter;

        private void osc_dir_par_input_Initialized(object sender, EventArgs e) => osc_dir_par_input.Text = Properties.audio_direction_parameter;

        private void save_config_Click(object sender, RoutedEventArgs e)
        {
            Properties.gain = (float)Gain.Value;
            Properties.audio_direction_parameter = osc_dir_par_input.Text.Trim();
            Properties.audio_volume_parameter = osc_vol_par_input.Text.Trim();
            int.TryParse(osc_port_input.Text, out Properties.port);
            OscUtility.SendPort = Properties.port;
            Properties.sentry_reporting_enabled = senry_enabled_checkbox.IsChecked ?? true;
            settings_window.IsPopupOpen = false;
        }

        private void osc_port_input_Initialized(object sender, EventArgs e) => osc_port_input.Text = Properties.port.ToString();

        private void senry_enabled_checkbox_Initialized(object sender, EventArgs e) => senry_enabled_checkbox.IsChecked = Properties.sentry_reporting_enabled;
    }
}