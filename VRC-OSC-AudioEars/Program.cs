﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using BuildSoft.VRChat.Osc;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Octokit;
using Sentry;

namespace VRC_OSC_AudioEars
{
    public static class VRChatOscAudio
    {
        
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        
        private const string AudioVolumeParameter = "audio_volume";
        private const string AudioDirectionParameter = "audio_direction";

        
        private static float _leftEarSmoothedVolume = 0;
        private static float _rightEarSmoothedVolume = 0;
        private static float _leftEarIncomingVolume = 0;
        private static float _rightEarIncomingVolume = 0;
        private static float _direction = 0;
        private static MMDeviceEnumerator enumerator = new();
        private static MMDevice? _device = null;
        private static WasapiLoopbackCapture? _capture = null;
        private static int _bytesPerSample = 0;
        

        public static async Task Main(string[] args)
        {
            Helpers.InitLogging();
            Console.Title = "VRChat OSC Audio Reaction";
            Logger.Info("Starting...");
            Helpers.InitSentry();
            Logger.Info($"Version: {Helpers.AssemblyProductVersion}");
            // Get Default audio device
            await SetUpAudio().ConfigureAwait(false);
            await CheckGitHubNewerVersion().ConfigureAwait(false); // Don't wait for it
            Logger.Info("Please enable OSC in the VRChat action menu.");


            while (true)
            {
                try
                {
                    _leftEarSmoothedVolume = Helpers.VRCClampedLerp(_leftEarSmoothedVolume, _leftEarIncomingVolume, 0.3f);
                    _rightEarSmoothedVolume = Helpers.VRCClampedLerp(_rightEarSmoothedVolume, _rightEarIncomingVolume, 0.3f);
                    if (float.IsNaN(_leftEarSmoothedVolume) || float.IsNaN(_rightEarSmoothedVolume) || float.IsInfinity(_leftEarSmoothedVolume) || float.IsInfinity(_rightEarSmoothedVolume))
                    {
                        // handle nan
                        _leftEarSmoothedVolume = 0;
                        _rightEarSmoothedVolume = 0;
                    }
                    _direction = Helpers.VRCClamp(-(_leftEarSmoothedVolume * 2) + (_rightEarSmoothedVolume * 2) + 0.5f);
                    OscParameter.SendAvatarParameter(AudioDirectionParameter, _direction);;
                    OscParameter.SendAvatarParameter(AudioVolumeParameter, (_leftEarSmoothedVolume + _rightEarSmoothedVolume) / 2);
                }
                catch (Exception e)
                {
                    // Often errors when trying to send a value while changing avatars
                    Console.WriteLine("Error: " + e.Message);
                    SentrySdk.CaptureException(e);
                    await Task.Delay(2000);
                }


                await Task.Delay(25);
                _shouldUpdate = true;

            }
        }

        private static float _leftRaw = 0;
        private static float _rightRaw = 0;
        private static bool _shouldUpdate = false;
        static void OnDataAvailable(object sender, WaveInEventArgs args)
        {
            if (_shouldUpdate)
            {
                _shouldUpdate = false;
                _leftRaw = 0;
                _rightRaw = 0;
                // get the RMS of the left and right channels independently for 32 bit audio. ALso Githun Copilot go burr
                if (_bytesPerSample == 4)
                {
                    for (int i = 0; i < args.BytesRecorded; i += _bytesPerSample * 2)
                    {
                        _leftRaw += Math.Abs(BitConverter.ToSingle(args.Buffer, i));
                        _rightRaw += Math.Abs(BitConverter.ToSingle(args.Buffer, i + _bytesPerSample));
                    }
                }
                else if (_bytesPerSample == 2)
                {
                    for (int i = 0; i < args.BytesRecorded; i += _bytesPerSample * 2)
                    {
                        _leftRaw += Math.Abs(BitConverter.ToInt16(args.Buffer, i));
                        _rightRaw += Math.Abs(BitConverter.ToInt16(args.Buffer, i + _bytesPerSample));
                    }
                }
                else
                {
                    for (int i = 0; i < args.BytesRecorded; i += _bytesPerSample)
                    {
                        _leftRaw += Math.Abs(BitConverter.ToInt32(args.Buffer, i));
                        _rightRaw += Math.Abs(BitConverter.ToInt32(args.Buffer, i + _bytesPerSample));
                    }
                }
                _leftEarIncomingVolume = _leftRaw / (args.BytesRecorded / _bytesPerSample);
                _rightEarIncomingVolume = _rightRaw / (args.BytesRecorded / _bytesPerSample);

                _leftEarIncomingVolume *= 10;
                _rightEarIncomingVolume *= 10;
            }
        }
        static void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            if (_capture != null) _capture.Dispose();
            if (_device != null) _device.Dispose();
            Environment.Exit(0);
        }
        public static Task SetUpAudio()
        {
            _device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            _capture = new WasapiLoopbackCapture(_device);
            _capture.DataAvailable += OnDataAvailable!;
            _capture.RecordingStopped += OnRecordingStopped!;
            _bytesPerSample = _capture.WaveFormat.BitsPerSample / 8;
            _capture.StartRecording();
            Logger.Info("Device: " + _device.FriendlyName + " Bitrate: " + _capture.WaveFormat.BitsPerSample + " SampleRate: " + _capture.WaveFormat.SampleRate);
            return Task.CompletedTask;
        }
        private static async Task CheckGitHubNewerVersion()
        {
            GitHubClient client = new GitHubClient(new ProductHeaderValue("SomeName"));
            IReadOnlyList<Release> releases = await client.Repository.Release.GetAll("Codel1417", "VRC-OSC-Audio-Reaction");
    
            if (Helpers.AssemblyProductVersion != "" && releases.Count > 0)
            {
                Version latestGitHubVersion = new Version(releases[0].TagName);
                Version localVersion = new Version(Helpers.AssemblyProductVersion); //Replace this with your local version. 
                int versionComparison = localVersion.CompareTo(latestGitHubVersion);
                if (versionComparison < 0)
                {
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
                SentrySdk.CaptureException(new VersionNotFoundException("Unable to check for updates."));

            }
        }
    }
}