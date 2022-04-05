using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using BuildSoft.VRChat.Osc;
using CommandLine;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NLog;
using Octokit;
using Sentry;

namespace VRC_OSC_AudioEars
{
    public static class VRChatOscAudio
    {
        // ReSharper disable MemberHidesStaticFromOuterClass
        public class Options
        {
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; }
            [Option('s',"sentry", Required = false, HelpText = "Disable Sentry error reporting.")]
            public bool DisableSentry { get; set; }
        }
        // ReSharper enable MemberHidesStaticFromOuterClass

        
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public static bool Verbose { get; set; }
        public static bool DisableSentry { get; set; }



        

        

        public static async Task Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options => RunOptions(options))
                .WithNotParsed(errors => HandleParseError(errors));
            Helpers.InitLogging(Verbose);
            Console.Title = "VRChat OSC Audio Reaction";
            Logger.Info("Starting...");
            if(!DisableSentry) await Helpers.InitSentry();
            Logger.Info($"Version: {Helpers.AssemblyProductVersion}");
            // Get Default audio device
            Audio audio = new();
            await audio.SetUpAudio().ConfigureAwait(true);
            await Helpers.CheckGitHubNewerVersion().ConfigureAwait(true); // Don't wait for it
            Logger.Info("Please enable OSC in the VRChat action menu.");
            await audio.Update();

        }
        
        private static void HandleParseError(IEnumerable<Error> obj)
        {
            Environment.Exit(1);
        }

        private static void RunOptions(Options obj)
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
    }
}