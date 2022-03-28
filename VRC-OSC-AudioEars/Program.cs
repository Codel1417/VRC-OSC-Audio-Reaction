using System;
using System.Threading.Tasks;
using BuildSoft.VRChat.Osc;
using BuildSoft.VRChat.Osc.Avatar;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace VRC_OSC_AudioEars
{
    public static class VRChatOscAudio
    {
        private const string AudioVolumeParameter = "audio_volume";
        private const string AudioDirectionParameter = "audio_direction";

        private static float _leftEarSmoothedVolume = 0;
        private static float _rightEarSmoothedVolume = 0;
        private static float _leftEarIncomingVolume = 0;
        private static float _rightEarIncomingVolume = 0;
        private static float _direction = 0;
        private static MMDeviceEnumerator enumerator = new();
        private static MMDevice _device = null;
        private static WasapiLoopbackCapture _capture = null;
        private static int _bytesPerSample = 0;
        public static async Task Main(string[] args)
        {
            Console.Title = "VRChat OSC Audio";
            Console.WriteLine("Starting...");
            // Get Default audio device
            _device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            _capture = new WasapiLoopbackCapture(_device);
            _capture.DataAvailable += OnDataAvailable;
            _capture.RecordingStopped += OnRecordingStopped;
            _bytesPerSample = _capture.WaveFormat.BitsPerSample / 8;
            _capture.StartRecording();

            Console.WriteLine("Device: " + _device.FriendlyName + " Bitrate: " + _capture.WaveFormat.BitsPerSample + " SampleRate: " + _capture.WaveFormat.SampleRate);

            Console.WriteLine("Waiting for VRChat... Please enable OSC in the VRChat action menu.");
            Console.WriteLine("If OSC is Already enabled, turn it off and back on.");
            // ReSharper disable once NotAccessedVariable
            OscAvatarConfig config = await OscAvatarConfig.WaitAndCreateAtCurrentAsync();
            Console.WriteLine("Started");


            while (true)
            {
                try
                {
                    config = await OscAvatarConfig.WaitAndCreateAtCurrentAsync();
                    _leftEarSmoothedVolume = Helpers.VRCClampedLerp(_leftEarSmoothedVolume, _leftEarIncomingVolume, 0.3f);
                    _rightEarSmoothedVolume = Helpers.VRCClampedLerp(_rightEarSmoothedVolume, _rightEarIncomingVolume, 0.3f);
                    if (float.IsNaN(_leftEarSmoothedVolume) || float.IsNaN(_rightEarSmoothedVolume) || float.IsInfinity(_leftEarSmoothedVolume) || float.IsInfinity(_rightEarSmoothedVolume))
                    {
                        // handle nan
                        _leftEarSmoothedVolume = 0;
                        _rightEarSmoothedVolume = 0;
                    }
                    _direction = Helpers.VRCClamp(-(_leftEarSmoothedVolume * 2) + (_rightEarSmoothedVolume * 2) + 0.5f);
                    //Write all values to the console
                    OscParameter.SendAvatarParameter(AudioDirectionParameter, _direction);;
                    OscParameter.SendAvatarParameter(AudioVolumeParameter, (_leftEarSmoothedVolume + _rightEarSmoothedVolume) / 2);
                }
                catch (Exception e)
                {
                    // Often errors when trying to send a value while changing avatars
                    Console.WriteLine("Error: " + e.Message);
                    await Task.Delay(2000);

                }


                await Task.Delay(25);
                _shouldUpdate = true;

            }
        }

        private static float _left = 0;
        private static float _right = 0;
        private static bool _shouldUpdate = false;
        static void OnDataAvailable(object sender, WaveInEventArgs args)
        {
            if (_shouldUpdate)
            {
                _shouldUpdate = false;
                _left = 0;
                _right = 0;
                // get the RMS of the left and right channels independently for 32 bit audio. ALso Githun Copilot go burr
                if (_bytesPerSample == 4)
                {
                    for (int i = 0; i < args.BytesRecorded; i += _bytesPerSample * 2)
                    {
                        _left += Math.Abs(BitConverter.ToSingle(args.Buffer, i));
                        _right += Math.Abs(BitConverter.ToSingle(args.Buffer, i + _bytesPerSample));
                    }
                }
                else if (_bytesPerSample == 2)
                {
                    for (int i = 0; i < args.BytesRecorded; i += _bytesPerSample * 2)
                    {
                        _left += Math.Abs(BitConverter.ToInt16(args.Buffer, i));
                        _right += Math.Abs(BitConverter.ToInt16(args.Buffer, i + _bytesPerSample));
                    }
                }
                else
                {
                    for (int i = 0; i < args.BytesRecorded; i += _bytesPerSample)
                    {
                        _left += Math.Abs(BitConverter.ToInt32(args.Buffer, i));
                        _right += Math.Abs(BitConverter.ToInt32(args.Buffer, i + _bytesPerSample));
                    }
                }
                _leftEarIncomingVolume = _left / (args.BytesRecorded / _bytesPerSample);
                _rightEarIncomingVolume = _right / (args.BytesRecorded / _bytesPerSample);

                _leftEarIncomingVolume *= 10;
                _rightEarIncomingVolume *= 10;
            }
        }
        static void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            _capture.Dispose();
            _device.Dispose();
            Environment.Exit(0);
        }

    }
}