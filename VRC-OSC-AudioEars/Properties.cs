using Newtonsoft.Json;
using NLog;
using Sentry;
using System;
using System.IO;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace VRC_OSC_AudioEars
{
    static class Properties
    {
        public static float gain = 1;
        public static bool enabled = true;
        public static int port = 9000;
        public static string audio_direction_parameter = "audio_direction";
        public static string audio_volume_parameter = "audio_volume";
        public static bool sentry_reporting_enabled = true;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static void reset()
        {
            gain = 1;
            port = 9000;
            audio_direction_parameter = "audio_direction";
            audio_volume_parameter = "audio_volume";
            sentry_reporting_enabled = true;
        }

        public static async Task Load()
        {
            Logger.Debug("Loading settings");
            String path = Environment.ExpandEnvironmentVariables(Constants.config_Path);
            if (File.Exists(path))
            {
                using StreamReader streamReader = new StreamReader(path, System.Text.Encoding.UTF8);
                {
                                    String line = await streamReader.ReadToEndAsync();
                var definition = new
                {
                    gain,
                    port,
                    audio_direction_parameter,
                    audio_volume_parameter,
                    sentry_reporting_enabled
                };
                try
                {
                    var thisObject = JsonConvert.DeserializeAnonymousType(line, definition);
                    if (thisObject != null)
                    {
                        gain = thisObject.gain;
                        port = thisObject.port;
                        audio_direction_parameter = thisObject.audio_direction_parameter;
                        audio_volume_parameter = thisObject.audio_volume_parameter;
                        sentry_reporting_enabled = thisObject.sentry_reporting_enabled;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Unable to load settings");
                    SentrySdk.CaptureException(ex);
                }
                }
            }

        }
        public static void Save()
        {
            try
            {
                Logger.Debug("Saving settings");
                String path = Environment.ExpandEnvironmentVariables(Constants.config_Path);
                Logger.Trace("Create directory if not exists");
                Directory.CreateDirectory(path);

                FileStream fs = new(Path.Combine(path, Constants.config_file),
                                               FileMode.Create, FileAccess.ReadWrite);
                using StreamWriter file = new(fs, System.Text.Encoding.UTF8);
                {
                    using (JsonWriter jw = new JsonTextWriter(file))
                    {
                        JsonSerializerSettings jsonSerializerSettings = new();
                        JsonSerializer serializer = new();
                        serializer.Serialize(jw,
                            new
                            {
                                gain,
                                port,
                                audio_direction_parameter,
                                audio_volume_parameter,
                                sentry_reporting_enabled
                            });
                    }
                }
            } catch (Exception ex)
            {
                Logger.Error(ex, "Unable to Save settings");
                SentrySdk.CaptureException(ex);
            }
        }

    }
}
