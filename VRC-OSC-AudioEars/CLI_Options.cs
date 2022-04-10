using CommandLine;

namespace VRC_OSC_AudioEars
{
    public class CLI_Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }
        [Option('s', "no-sentry", Required = false, HelpText = "Disable Sentry error reporting.")]
        public bool DisableSentry { get; set; }
    }
}