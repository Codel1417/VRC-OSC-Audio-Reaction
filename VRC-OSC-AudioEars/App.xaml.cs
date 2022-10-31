using System.Diagnostics;
using System.Threading;
using Sentry;
using System.Windows;
using System.Windows.Threading;
using NLog;
using System.IO;
using System;

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
        /// <summary>
        /// Sets or replaces the ResourceDictionary by dynamically loading
        /// a Localization ResourceDictionary from the file path passed in.
        /// </summary>
        /// <param name="resourceDictionaryFile">The resource dictionary to use to set/replace
        /// the ResourceDictionary.</param>
        private void SetCultureResourceDictionary(string resourceDictionaryFile)
        {
            // Scan all resource dictionaries and remove, if it is string resource distionary
            for (int index = 0; index < Resources.MergedDictionaries.Count; ++index)
            {
                // Look for an indicator in the resource file that indicates the resource is
                // swappable. For instance in our files the header contains this: 
                // <sys:String x:Key="ResourceDictionaryName">Resources-en-CA</sys:String> 
                if (Resources.MergedDictionaries[index].Contains("Strings"))
                {
                    if (File.Exists(resourceDictionaryFile))
                    {
                        Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[index]);
                    }
                }
            }

            // read required resource file to resource dictionary and add to MergedDictionaries collection
            ResourceDictionary newResourceDictionary = new()
            {
                Source = new Uri(resourceDictionaryFile)
            };
            Resources.MergedDictionaries.Add(newResourceDictionary);
        }
    }


}