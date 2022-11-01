using Octokit;
using Sentry;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using VRC_OSC_AudioEars.Properties;

namespace VRC_OSC_AudioEars
{
    public static class Helpers
    {
        private static MainWindow? mainWindow;

        /// <summary>
        /// Linear interpolation between two values.
        /// </summary>
        /// <param name="firstFloat">The Left Value (0)</param>
        /// <param name="secondFloat">The Right Value (1)</param>
        /// <param name="by">Lerp Point</param>
        /// <returns></returns>
        /// 
        public static float Lerp(float firstFloat, float secondFloat, float by)
        {
            return firstFloat * (1 - by) + secondFloat * by;
        }

        /// <summary>
        /// Clamps a float between 0 and 1, while preventing getting too precise when approaching zero. This is due to a bug in VRChat.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Clamped Value</returns>
        public static float VRCClamp(float value)
        {
            return value switch
            {
                < 0.005f => 0.005f,
                > 1f => 1f,
                _ => value
            };
        }

        public static float VRCClampedLerp(float firstFloat, float secondFloat, float by)
        {
            return VRCClamp(Lerp(firstFloat, secondFloat, by));
        }

        public static string AssemblyProductVersion
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly()
                    .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
                return attributes.Length == 0
                    ? ""
                    : ((AssemblyInformationalVersionAttribute)attributes[0]).InformationalVersion;
            }
        }

        public static bool IsDebugBuild
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }

        public static MainWindow? MainWindow { get => mainWindow; set => mainWindow = value; }

        public static Task InitSentry()
        {
            try
            {
                SentrySdk.Init(o =>
                {
                    o.Dsn = "https://c39539385af440b2854d2d558c4d8d82@o1187002.ingest.sentry.io/6307588";
                    o.Environment = IsDebugBuild ? "Debug" : "Release";
                    o.Debug = IsDebugBuild;
                    o.TracesSampleRate = 0;
                    o.AttachStacktrace = true;
                    o.UseAsyncFileIO = true;
                    o.ReportAssembliesMode = ReportAssembliesMode.InformationalVersion;
                    o.DetectStartupTime = StartupTimeDetectionMode.Best;
                    o.StackTraceMode = StackTraceMode.Enhanced;
                    o.RequestBodyCompressionLevel = CompressionLevel.SmallestSize;
                    o.DecompressionMethods = System.Net.DecompressionMethods.Deflate;
                    o.EnableScopeSync = true;
                    o.Release = AssemblyProductVersion;
                });
                SentrySdk.ConfigureScope(scope =>
                {
                    scope.SetTag("os", Environment.OSVersion.ToString());
                    scope.SetTag("arch", Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit");
                });
                return Task.FromResult(Task.CompletedTask);
            }
            catch (Exception)
            {
                // No one to read the log
            }
            return Task.CompletedTask;
        }

        public static async Task CheckGitHubNewerVersion()
        {

            SentrySdk.AddBreadcrumb("Checking GitHub for newer version");
            try
            {
                GitHubClient client = new(new ProductHeaderValue("VRC-OSC-Audio-Reaction"));
                IReadOnlyList <Release> releases =
                    await client.Repository.Release.GetAll("Codel1417", "VRC-OSC-Audio-Reaction");
                if (AssemblyProductVersion != "" && releases.Count > 0)
                {
                    Version latestGitHubVersion = new(releases[0].TagName);
                    Version localVersion = new(Helpers.AssemblyProductVersion);
                    int versionComparison = localVersion.CompareTo(latestGitHubVersion);
                    if (versionComparison < 0)
                    {
                        if (MainWindow != null) await MainWindow.Dispatcher.InvokeAsync(new Action(() => MainWindow.SnackBar.MessageQueue?.Enqueue(Strings.updateMessage, Strings.updateGo, async _ => await Windows.System.Launcher.LaunchUriAsync(new Uri(releases[0].HtmlUrl)), true, true, false, TimeSpan.FromSeconds(15))));
                    }
                }
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
            }

            SentrySdk.AddBreadcrumb("Ending check for updates");
        }

    }
}