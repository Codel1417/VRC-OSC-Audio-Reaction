using System;
using System.Diagnostics;
using Sentry;
using System.Windows;
using System.Windows.Threading;
using NLog;

namespace VRC_OSC_AudioEars
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public App()
        {
            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
            {
                Logger.Error("App already running!");
                Environment.Exit(1);
            }
            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            SentrySdk.CaptureException(e.Exception);

            e.Handled = true;
        }
    }


}