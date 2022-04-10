using System;

namespace VRC_OSC_AudioEars
{
    [Serializable()]
    static class Properties
    {
        public static float gain = 1;
        public static bool enabled = true;
        public static int port = 9000;
        public static string audio_direction_parameter = "audio_direction";
        public static string audio_volume_parameter = "audio_volume";
        public static bool sentry_reporting_enabled = true;

        public static void reset()
        {
            gain = 1;
            port = 9000;
            audio_direction_parameter = "audio_direction";
            audio_volume_parameter = "audio_volume";
            sentry_reporting_enabled = true;
        }
    }
}
