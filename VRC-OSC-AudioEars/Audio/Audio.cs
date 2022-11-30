using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using BuildSoft.VRChat.Osc;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using VRC_OSC_AudioEars.Properties;

namespace VRC_OSC_AudioEars.Audio;

internal sealed class Audio
{
    private Audio()
    {
    }

    private float _gain = 1;
    internal static readonly ConcurrentQueue<Action> Queue = new();
    private static Audio? _instance = null;
    private bool _enabled = false;
    private bool _stop = false;
    public static Audio Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new Audio();
            }

            return _instance;
        }
    }

    private float _leftRaw;
    private float _rightRaw;
    private bool _shouldUpdate;
    
    private float _leftEarSmoothedVolume;
    private float _rightEarSmoothedVolume;

    private float _leftEarIncomingVolume;
    private float _rightEarIncomingVolume;
    private float _direction = 0;
    private float _previousDirection = 0;
    private float _volume = 0;
    private float _previousVolume = 0;
    private readonly MMDeviceEnumerator _enumerator = new();
    private MMDevice? _activeDevice;
    private WasapiLoopbackCapture? _capture;
    private int _bytesPerSample;

    private void OnDataAvailable(object? sender, WaveInEventArgs args)
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

            _leftEarIncomingVolume = _leftRaw / (args.BytesRecorded / _bytesPerSample);
            _rightEarIncomingVolume = _rightRaw / (args.BytesRecorded / _bytesPerSample);

            // Boost volume to usable level
            _leftEarIncomingVolume *= 10;
            _rightEarIncomingVolume *= 10;
        }
    }

    public void UpdateDefaultDeviceUI()
    {
        MMDevice defaultDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
        string defaultDeviceName = defaultDevice.FriendlyName;

        Helpers.MainWindow?.Dispatcher.InvokeAsync(new Action(() =>
            Helpers.MainWindow.DeviceName.SelectedIndex =
                Helpers.MainWindow.DeviceName.Items.IndexOf(defaultDeviceName)));
    }

    public void UpdateUiDeviceList()
    {
        ComboBox? combobox = null;
        Helpers.MainWindow?.Dispatcher.Invoke(new Action(() =>
            combobox = Helpers.MainWindow.DeviceName));
        if (combobox != null)
        {
            Helpers.MainWindow?.Dispatcher.Invoke(() =>
                Helpers.MainWindow.DeviceName.Items.Clear());

            IEnumerable<MMDevice> devices = GetDeviceList();
            MMDevice defaultDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            foreach (string? name in devices.Select(wasapi => wasapi.FriendlyName))
            {
                Helpers.MainWindow?.Dispatcher.Invoke(() =>
                    Helpers.MainWindow.DeviceName.Items.Add(name));
            }

            int index = -1;
            Helpers.MainWindow?.Dispatcher.Invoke(() =>
                index = Helpers.MainWindow.DeviceName.SelectedIndex);
            if (index == -1)
            {
                string defaultDeviceName = defaultDevice.FriendlyName;

                Helpers.MainWindow?.Dispatcher.Invoke(() =>
                    Helpers.MainWindow.DeviceName.SelectedIndex ==
                    Helpers.MainWindow.DeviceName.Items.IndexOf(defaultDeviceName));
            }
        }
    }

    private IEnumerable<MMDevice> GetDeviceList()
    {
        List<MMDevice> mMDevices = new();
        foreach (var wasapi in _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
        {
            if (wasapi != null &&
                wasapi.AudioEndpointVolume.HardwareSupport > 0) // covers up crash when virtual audio device is selected
                mMDevices.Add(wasapi);
        }

        return mMDevices;
    }

    private MMDevice? GetSelectedDevice(string? selectedItem)
    {
        if (selectedItem != null)
        {
            foreach (var device in GetDeviceList().Where(device =>
                         device.FriendlyName.Equals(selectedItem) && device.State == DeviceState.Active))
            {
                return device;
            }

            return _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }

        return null;
    }

    private bool _registerListener;

    public void SetUpAudio(string? selectedItem)
    {
        if (!_registerListener)
        {
            //Causes crash when switching device, not the handling just registering the callback :/
            _enumerator.RegisterEndpointNotificationCallback(new AudioEventListener());
            _registerListener = true;
        }

        MMDevice? newDevice = GetSelectedDevice(selectedItem);
        try
        {
            if (newDevice != null && _activeDevice != null && _activeDevice.FriendlyName == newDevice.FriendlyName &&
                _capture is { CaptureState: CaptureState.Capturing })
            {
                return;
            }


            _capture?.StopRecording();
            _capture?.Dispose();
            if (_capture != null) _capture.DataAvailable -= OnDataAvailable;
            _capture = null;
            _activeDevice?.Dispose();
            _activeDevice = newDevice;
            if (_activeDevice != null)
            {
                if (_activeDevice.AudioEndpointVolume.HardwareSupport == 0)
                {
                    throw new NotSupportedException(
                        "Selected device does not support capturing"); // covers up crash when virtual audio device is selected
                }

                _capture = new WasapiLoopbackCapture(_activeDevice);
                _capture.DataAvailable += OnDataAvailable;
                _capture.WaveFormat =
                    WaveFormat.CreateIeeeFloatWaveFormat(48000, 2); // Use a consistent format for processing audio
                _bytesPerSample = _capture.WaveFormat.BitsPerSample / _capture.WaveFormat.BlockAlign;

                _capture.StartRecording();

                OscUtility.SendPort = Settings.Default.port;
            }
        }
        catch (Exception)
        {
            Settings.Default.enabled = false;
        }
    }

    public void Enable() => _enabled = true;
    public void Disable() => _enabled = false;
    public void StopLoop() => Instance._stop = true;
    public static void StartLoop()
    {
        Instance._stop = false;
        Instance.Update().ConfigureAwait(false);// main update loop
    }

    public void SetGain(float gain) => this._gain = gain;

    private async Task Update()
    {
        while (!_stop)
        {
            // Command queue
            Queue.TryDequeue(out var data);
            data?.Invoke();
            if (!_enabled)
            {
                await Task.Delay(25);
                continue;
            }
            
            try
            {
                _leftEarSmoothedVolume = VRCClampedLerp(_leftEarSmoothedVolume,
                    _leftEarIncomingVolume * _gain, 0.3f);
                _rightEarSmoothedVolume = VRCClampedLerp(_rightEarSmoothedVolume,
                    _rightEarIncomingVolume * _gain, 0.3f);
                if (float.IsNaN(_leftEarSmoothedVolume) || float.IsNaN(_rightEarSmoothedVolume) ||
                    float.IsInfinity(_leftEarSmoothedVolume) || float.IsInfinity(_rightEarSmoothedVolume))
                {
                    // handle nan
                    _leftEarSmoothedVolume = 0;
                    _rightEarSmoothedVolume = 0;
                }

                _direction = VRCClamp(-(_leftEarSmoothedVolume * 2) + (_rightEarSmoothedVolume * 2) + 0.5f);
                //log values with fixed decimal places
                _volume = (_leftEarSmoothedVolume + _rightEarSmoothedVolume) / 2f;

                if (Math.Abs(_direction - _previousDirection) > 0.001f)
                {
                    OscParameter.SendAvatarParameter(Settings.Default.audio_direction, _direction);
                    _previousDirection = _direction;
                }

                if (Math.Abs(_volume - _previousVolume) > 0.001f)
                {
                    OscParameter.SendAvatarParameter(Settings.Default.audio_volume,_volume);
                    _previousVolume = _volume;
                }
            }
            catch (Exception)
            {
                // Often errors when trying to send a value while changing avatars
                await Task.Delay(2000);
            }

            UpdateUi();
            await Task.Delay(50);
            _shouldUpdate = true;
        }
    }

    private void UpdateUi()
    {
        Helpers.MainWindow?.Dispatcher.InvokeAsync(new Action(() =>
            Helpers.MainWindow.LeftAudioMeter.Value = _leftEarSmoothedVolume));
        Helpers.MainWindow?.Dispatcher.InvokeAsync(new Action(() =>
            Helpers.MainWindow.RightAudioMeter.Value = _rightEarSmoothedVolume));
    }


    /// <summary>
    /// Clamps a float between 0 and 1, while preventing getting too precise when approaching zero. This is due to a bug in VRChat.
    /// </summary>
    /// <param name="value"></param>
    /// <returns>Clamped Value</returns>
    private static float VRCClamp(float value)
    {
        return value switch
        {
            < 0.005f => 0.005f,
            > 1f => 1f,
            _ => value
        };
    }

    private static float VRCClampedLerp(float firstFloat, float secondFloat, float by)
    {
        return VRCClamp(firstFloat * (1 - by) + secondFloat * by);
    }
}