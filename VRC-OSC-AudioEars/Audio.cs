using System;
using System.Threading.Tasks;
using BuildSoft.VRChat.Osc;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NLog;
using Sentry;

namespace VRC_OSC_AudioEars;

public class Audio
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private float _leftRaw = 0;
        private float _rightRaw = 0;
        private bool _shouldUpdate = false;
        private float _leftEarSmoothedVolume = 0;
        private float _rightEarSmoothedVolume = 0;
        private float _leftEarIncomingVolume = 0;
        private float _rightEarIncomingVolume = 0;
        private float _direction = 0;
        private MMDeviceEnumerator enumerator = new();
        private MMDevice? _device = null;
        private WasapiLoopbackCapture? _capture = null;
        private int _bytesPerSample = 0;

        public MainWindow? mainWindow;
        private void OnDataAvailable(object sender, WaveInEventArgs args)
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
        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            Logger.Info("Ending recording");
            if (_capture != null) _capture.Dispose();
            if (_device != null) _device.Dispose();
            Environment.Exit(0);
        }
        public Task SetUpAudio()
        {
            Logger.Debug("Setting up audio");
            Logger.Trace("Getting default audio device");
            _device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            Logger.Trace("Setting up Loopback capture");
            _capture = new WasapiLoopbackCapture(_device);
            Logger.Trace("Setting up Event listeners");
            _capture.DataAvailable += OnDataAvailable!;
            _capture.RecordingStopped += OnRecordingStopped!;
            _bytesPerSample = _capture.WaveFormat.BitsPerSample / 8;
            Logger.Trace("Starting capture");
            _capture.StartRecording();
            Logger.Info("Device: " + _device.FriendlyName + " Bitrate: " + _capture.WaveFormat.BitsPerSample + " SampleRate: " + _capture.WaveFormat.SampleRate);
            mainWindow.Dispatcher.InvokeAsync(new Action(() => mainWindow.deviceName.Text = _device.DeviceFriendlyName));

            Logger.Debug("Configuring Sentry scope");
            
            SentrySdk.ConfigureScope(scope => scope.Contexts["Audio Device"] = new
            {
                _device.FriendlyName,
                _capture.WaveFormat.BitsPerSample,
                _capture.WaveFormat.SampleRate,
                _capture.WaveFormat.Channels,
                Encoding=_capture.WaveFormat.Encoding.ToString(),
                CaptureState = _capture.CaptureState.ToString(),
                ShareMode = _capture.ShareMode.ToString(),
                _capture.WaveFormat.BlockAlign,
                _capture.WaveFormat.AverageBytesPerSecond,
                DeviceState = _device.State.ToString(),
            });
            SentrySdk.AddBreadcrumb(
                message: "Audio Set Up " + _device.FriendlyName,
                category: "Audio",
                level: BreadcrumbLevel.Info);

        OscUtility.SendPort = Properties.port;

        return Task.CompletedTask;
        }

        public async Task Update()
        {
            while (true)
            {
                if (Properties.enabled == false) {
                    await Task.Delay(25);
                    continue;
                }



                try
                {
                    _leftEarSmoothedVolume = Helpers.VRCClampedLerp(_leftEarSmoothedVolume, _leftEarIncomingVolume * Properties.gain, 0.3f);
                    _rightEarSmoothedVolume = Helpers.VRCClampedLerp(_rightEarSmoothedVolume, _rightEarIncomingVolume * Properties.gain, 0.3f);
                    if (float.IsNaN(_leftEarSmoothedVolume) || float.IsNaN(_rightEarSmoothedVolume) || float.IsInfinity(_leftEarSmoothedVolume) || float.IsInfinity(_rightEarSmoothedVolume))
                    {
                        // handle nan
                        _leftEarSmoothedVolume = 0;
                        _rightEarSmoothedVolume = 0;
                    }
                    _direction = Helpers.VRCClamp(-(_leftEarSmoothedVolume * 2) + (_rightEarSmoothedVolume * 2) + 0.5f);
                    //log values with fixed decimal places
                    //Logger.Trace($"Left Ear: {_leftEarSmoothedVolume:F3} Right Ear: {_rightEarSmoothedVolume:F3} Direction: {_direction:F3}");
                    OscParameter.SendAvatarParameter(Properties.audio_direction_parameter, _direction);;
                    OscParameter.SendAvatarParameter(Properties.audio_volume_parameter, (_leftEarSmoothedVolume + _rightEarSmoothedVolume) / 2);
                }
                catch (Exception e)
                {
                    // Often errors when trying to send a value while changing avatars
                    Logger.Error(e, "Error sending OSC");
                    SentrySdk.CaptureException(e);
                    await Task.Delay(2000);
                }

                UpdateUI();
                await Task.Delay(25);
                _shouldUpdate = true;

            }
        }
    public void UpdateUI()
    {
        mainWindow.Dispatcher.InvokeAsync(new Action(() => mainWindow.leftAudioMeter.Value = _leftEarSmoothedVolume));
        mainWindow.Dispatcher.InvokeAsync(new Action(() => mainWindow.rightAudioMeter.Value = _rightEarSmoothedVolume));

    }
}