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

        private void OnGainSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Properties.gain = (float) e.NewValue;
        }

        private void OnErrorReportingEnabled(object sender, RoutedEventArgs e)
        {
            DisableSentry = false;
        }

        private void OnErrorReportingUnchecked(object sender, RoutedEventArgs e)
        {
            DisableSentry = true;
        }


        private void OnEnableApp(object sender, RoutedEventArgs e)
        {
            Properties.enabled = true;
        }

        private void OnDisableApp(object sender, RoutedEventArgs e)
        {
            Properties.enabled = false;

        }

        private async void Window_Initialized(object sender, EventArgs e)
        {
            audio.mainWindow = this;
            Logger.Info($"Version: {Helpers.AssemblyProductVersion}");
            string[] args = Environment.GetCommandLineArgs();
            Parser.Default.ParseArguments<CLI_Options>(args)
                .WithParsed(options => RunOptions(options))
                .WithNotParsed(errors => HandleParseError(errors));
            if (!DisableSentry) await Helpers.InitSentry();
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
                DisableSentry = true;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void OnPortInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = int.TryParse(e.Text, out Properties.port);
            if (e.Handled)
            {
                OscUtility.SendPort = Properties.port;
            }
        }

            private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}