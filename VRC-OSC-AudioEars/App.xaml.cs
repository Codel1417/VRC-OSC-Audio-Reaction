using System.Diagnostics;
using System.Threading;
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
        private static readonly string AppGuid = "10935FB2-C97A-4FBF-B793-B092EC2D5C4E";
        private static readonly Mutex Mutex = new(false, "Global\\" + AppGuid);
        public App()
        {
            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
            {
                Logger.Error("App already running!");
                Current.Shutdown();
            }
            
                if(!Mutex.WaitOne(0, false))
                {
                    Logger.Error("App already running!");
                    Current.Shutdown();
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