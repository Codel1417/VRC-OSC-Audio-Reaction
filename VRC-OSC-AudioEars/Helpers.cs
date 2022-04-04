using System;
using System.Reflection;
using Sentry;
namespace VRC_OSC_AudioEars
{
    public static class Helpers
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Linear interpolation between two values.
        /// </summary>
        /// <param name="firstFloat">The Left Value (0)</param>
        /// <param name="secondFloat">The Right Value (1)</param>
        /// <param name="by">Lerp Point</param>
        /// <returns></returns>
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
                < 0.01f => 0.01f,
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

        public static void InitSentry()
        {
            SentrySdk.Init(o  =>
                   {
                       o.Dsn = "https://c39539385af440b2854d2d558c4d8d82@o1187002.ingest.sentry.io/6307588";
                       o.Environment = IsDebugBuild ? "Debug" : "Release";
                       o.Debug = IsDebugBuild;
                       o.TracesSampleRate = 1.0;
                       o.AttachStacktrace = true;
                   });
            SentrySdk.ConfigureScope(scope =>
            {
                scope.SetTag("os", Environment.OSVersion.ToString());
                scope.SetTag("arch", Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit");
                scope.SetTag("version", AssemblyProductVersion);
                scope.Platform = Environment.OSVersion.Platform.ToString();
                scope.SetTag("build_type", IsDebugBuild ? "Debug" : "Release");
                scope.SetExtra("CommandLine", Environment.CommandLine);
            });
            {
                Logger.Info(SentrySdk.IsEnabled ? "Sentry is enabled" : "Sentry is disabled");
            }
        }
    }
}