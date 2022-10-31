using NLog;
using NLog.Conditions;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using Octokit;
using Sentry;
using Sentry.NLog;
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
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public static MainWindow? mainWindow;

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

        public static Task InitSentry()
        {
            try
            {
                SentrySdk.Init(o =>
                {
                    o.Dsn = Constants.SENTRY_DSN;
                    o.Environment = IsDebugBuild ? "Debug" : "Release";
                    o.Debug = IsDebugBuild;
                    o.TracesSampleRate = 0;
                    o.AttachStacktrace = true;
                    o.UseAsyncFileIO = true;
                    o.ReportAssembliesMode = ReportAssembliesMode.InformationalVersion;
                    o.DetectStartupTime = StartupTimeDetectionMode.Best;
                    o.StackTraceMode = StackTraceMode.Enhanced;
                    o.AddDiagnosticSourceIntegration();
                    o.RequestBodyCompressionLevel = CompressionLevel.SmallestSize;
                    o.DecompressionMethods = System.Net.DecompressionMethods.Deflate;
                    o.EnableScopeSync = true;

                });
                SentrySdk.ConfigureScope(scope =>
                {
                    scope.SetTag("os", Environment.OSVersion.ToString());
                    scope.SetTag("arch", Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit");
                });
                return Task.FromResult(Task.CompletedTask);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to initialize Sentry");
            }
            Logger.Info(SentrySdk.IsEnabled ? "Sentry is enabled" : "Sentry is disabled");
            return Task.CompletedTask;
        }

        public static void InitLogging(bool verbose)
        {
            LoggingConfiguration config = new();

            ColoredConsoleTarget consoleTarget = new();
            ConsoleRowHighlightingRule hightlightError = new()
            {
                Condition = ConditionParser.ParseExpression("level == LogLevel.Error"),
                ForegroundColor = ConsoleOutputColor.Red
            };
            consoleTarget.RowHighlightingRules.Add(hightlightError);
            ConsoleRowHighlightingRule hightlightWarn = new()
            {
                Condition = ConditionParser.ParseExpression("level == LogLevel.Warn"),
                ForegroundColor = ConsoleOutputColor.Yellow
            };
            consoleTarget.RowHighlightingRules.Add(hightlightWarn);
            ConsoleRowHighlightingRule hightlightInfo = new()
            {
                Condition = ConditionParser.ParseExpression("level == LogLevel.Info"),
                ForegroundColor = ConsoleOutputColor.White
            };
            consoleTarget.RowHighlightingRules.Add(hightlightInfo);
            ConsoleRowHighlightingRule hightlightDebug = new()
            {
                Condition = ConditionParser.ParseExpression("level == LogLevel.Debug"),
                ForegroundColor = ConsoleOutputColor.Gray
            };
            consoleTarget.RowHighlightingRules.Add(hightlightDebug);
            consoleTarget.Layout = "${level}: ${message}";


            SentryTarget sentryTarget = new();
            sentryTarget.InitializeSdk = false;
            sentryTarget.MinimumBreadcrumbLevel = LogLevel.Debug.ToString();
            DiagnosticListenerTarget diagnosticListenerTarget = new();
            TraceTarget traceTarget = new();

            AsyncTargetWrapper asyncTraceTareget = new();
            asyncTraceTareget.WrappedTarget = traceTarget;
            AsyncTargetWrapper asyncdiagnosticListenerTarget = new();
            asyncdiagnosticListenerTarget.WrappedTarget = diagnosticListenerTarget;
            AsyncTargetWrapper asyncConsoleTarget = new();
            asyncConsoleTarget.WrappedTarget = consoleTarget;
            AsyncTargetWrapper asyncSentryTarget = new();
            asyncSentryTarget.WrappedTarget = sentryTarget;

            config.AddTarget("trace", asyncTraceTareget);
            config.AddTarget("diagnostic", asyncdiagnosticListenerTarget);
            config.AddTarget("console", asyncConsoleTarget);
            config.AddTarget("sentry", asyncSentryTarget);
            config.AddRule(verbose ? LogLevel.Trace : LogLevel.Info, LogLevel.Fatal, asyncdiagnosticListenerTarget);
            config.AddRule(verbose ? LogLevel.Trace : LogLevel.Info, LogLevel.Fatal, asyncConsoleTarget);
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, asyncTraceTareget);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, asyncSentryTarget);
            LogManager.Configuration = config;
        }

        public static async Task CheckGitHubNewerVersion()
        {

            Logger.Debug("Checking GitHub for newer version");
            try
            {
                Logger.Trace("Setting up github client");
                GitHubClient client = new GitHubClient(new ProductHeaderValue(Constants.project_name));
                Logger.Trace("Getting latest release");
                IReadOnlyList<Release> releases =
                    await client.Repository.Release.GetAll(Constants.project_user, Constants.project_name);
                if (AssemblyProductVersion != "" && releases.Count > 0)
                {
                    Logger.Trace("Getting latest release version");
                    Version latestGitHubVersion = new(releases[0].TagName);
                    Logger.Trace("Getting local version");
                    Version localVersion = new(Helpers.AssemblyProductVersion);
                    Logger.Trace("Comparing versions");
                    int versionComparison = localVersion.CompareTo(latestGitHubVersion);
                    if (versionComparison < 0)
                    {
                        if (mainWindow != null) await mainWindow.Dispatcher.InvokeAsync(new Action(() => mainWindow.SnackBar.MessageQueue?.Enqueue(Strings.updateMessage, Strings.updateGo, async _ => await Windows.System.Launcher.LaunchUriAsync(new Uri(releases[0].HtmlUrl)), true, true, false, TimeSpan.FromSeconds(15))));
                        Logger.Warn("A new version of VRC-OSC-Audio-Reaction is available!");
                    }
                    else
                    {
                        Logger.Info("You are running the latest version of VRC-OSC-Audio-Reaction!");
                    }
                }
                else
                {
                    Logger.Error("Could not check for updates.");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not check for updates.");
            }

            Logger.Trace("Ending check for updates");
        }

    }
}