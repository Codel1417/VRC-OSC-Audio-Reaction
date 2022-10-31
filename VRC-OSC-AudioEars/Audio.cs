using BuildSoft.VRChat.Osc;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NLog;
using Sentry;
using System;
using System.CodeDom;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Windows.Foundation.Metadata;
using VRC_OSC_AudioEars.Properties;

namespace VRC_OSC_AudioEars;

[Threading(ThreadingModel.MTA)]
public class Audio
{
    private Audio()
    {
    }

    public static readonly ConcurrentQueue<Action> Queue = new();
    private static Audio? _instance = null;

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

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private float _leftRaw;
    private float _rightRaw;
    private bool _shouldUpdate;
    private float _leftEarSmoothedVolume;
    private float _rightEarSmoothedVolume;
    private float _leftEarIncomingVolume;
    private float _rightEarIncomingVolume;
    private float _direction = 0;
    private readonly MMDeviceEnumerator _enumerator = new();
    private MMDevice? _activeDevice;
    private WasapiLoopbackCapture? _capture;
    private int _bytesPerSample;
    public bool IsDefaultCurrent { get; private set; } = true;

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

    private bool IsDefaultDevice()
    {
        MMDevice defaultDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
        bool isDefault = false;
        string defaultDeviceName = defaultDevice.FriendlyName;
        Helpers.mainWindow?.Dispatcher.InvokeAsync(new Action(() =>
            isDefault = Helpers.mainWindow.DeviceName.SelectedIndex ==
                        Helpers.mainWindow.DeviceName.Items.IndexOf(defaultDeviceName)));

        return isDefault;
    }

    public void UpdateDefaultDevice()
    {
        MMDevice defaultDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
        Logger.Debug("Setting default device: " + defaultDevice.FriendlyName);
        string defaultDeviceName = defaultDevice.FriendlyName;

        Helpers.mainWindow?.Dispatcher.InvokeAsync(new Action(() =>
            Helpers.mainWindow.DeviceName.SelectedIndex =
                Helpers.mainWindow.DeviceName.Items.IndexOf(defaultDeviceName)));
        IsDefaultCurrent = true;
    }

    public void UpdateUiDeviceList()
    {
        Logger.Debug("Updating UI device list");
        ComboBox? combobox = null;
        Helpers.mainWindow?.Dispatcher.Invoke(new Action(() =>
            combobox = Helpers.mainWindow.DeviceName));
        if (combobox != null)
        {
            Helpers.mainWindow?.Dispatcher.Invoke(() =>
                Helpers.mainWindow.DeviceName.Items.Clear());

            IEnumerable<MMDevice> devices = GetDeviceList();
            MMDevice defaultDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            foreach (string? name in devices.Select(wasapi => wasapi.FriendlyName))
            {
                Helpers.mainWindow?.Dispatcher.Invoke(() =>
                    Helpers.mainWindow.DeviceName.Items.Add(name));
            }

            int index = -1;
            Helpers.mainWindow?.Dispatcher.Invoke(() =>
                index = Helpers.mainWindow.DeviceName.SelectedIndex);
            if (index == -1 )
            {
                Logger.Debug("Setting default device: " + defaultDevice.FriendlyName);
                string defaultDeviceName = defaultDevice.FriendlyName;

                Helpers.mainWindow?.Dispatcher.Invoke(() =>
                    Helpers.mainWindow.DeviceName.SelectedIndex ==
                    Helpers.mainWindow.DeviceName.Items.IndexOf(defaultDeviceName));
                IsDefaultCurrent = true;
            }
        }
    }

    private IEnumerable<MMDevice> GetDeviceList()
    {
        Logger.Info("Getting list of output devices");
        List<MMDevice> mMDevices = new();
        foreach (var wasapi in _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
        {
            if (wasapi != null && wasapi.AudioEndpointVolume.HardwareSupport  > 0) // covers up crash when virtual audio device is selected
                mMDevices.Add(wasapi);
        }

        return mMDevices;
    }

    private MMDevice? GetSelectedDevice(string? selectedItem)
    {
        Logger.Info("Getting selected device: " + selectedItem);
        if (selectedItem != null)
        {
            foreach (var device in GetDeviceList().Where(device =>
                         device.FriendlyName.Equals(selectedItem) && device.State == DeviceState.Active))
            {
                Logger.Debug("Found matching device: " + device.FriendlyName);
                IsDefaultCurrent = IsDefaultDevice();
                return device;
            }

            UpdateDefaultDevice();
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
            Logger.Debug("Setting up audio");
            Logger.Trace("Getting audio device");
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
                Logger.Trace("Setting up Loopback capture");
                _capture = new WasapiLoopbackCapture(_activeDevice);
                if (_activeDevice.AudioEndpointVolume.HardwareSupport == 0)
                {
                    throw new NotSupportedException("Selected device does not support capturing"); // covers up crash when virtual audio device is selected
                }
                Logger.Trace("Setting up Event listeners");
                _capture.DataAvailable += OnDataAvailable;
                _capture.WaveFormat =
                    WaveFormat.CreateIeeeFloatWaveFormat(48000, 2); // Use a consistent format for processing audio
                _bytesPerSample = _capture.WaveFormat.BitsPerSample / _capture.WaveFormat.BlockAlign;


                Logger.Debug("Configuring Sentry scope");
                SentrySdk.ConfigureScope(scope => scope.Contexts["Audio Device"] = new
                {
                    _activeDevice.FriendlyName,
                    CaptureState = _capture.CaptureState.ToString(),
                    ShareMode = _capture.ShareMode.ToString(),
                    _capture.WaveFormat.AverageBytesPerSecond,
                    DeviceState = _activeDevice.State.ToString(),
                });
                

                Logger.Trace("Starting capture");

                _capture.StartRecording();
                Logger.Trace("Started capture");

                SentrySdk.AddBreadcrumb(
                    message: "Audio Set Up " + _activeDevice.FriendlyName,
                    category: "Audio",
                    level: BreadcrumbLevel.Info);

                OscUtility.SendPort = Settings.Default.port;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            SentrySdk.CaptureException(ex);
            Helpers.mainWindow?.Dispatcher.InvokeAsync(() =>
                    Helpers.mainWindow.SnackBar.MessageQueue?.Enqueue(Strings.deviceError));
            

            Settings.Default.enabled = false;
        }
    }

    public async Task Update()
    {
        while (true)
        {
            if (!Settings.Default.enabled)
            {
                await Task.Delay(25);
                continue;
            }

            Queue.TryDequeue(out var data);
            data?.Invoke();

            try
            {
                _leftEarSmoothedVolume = Helpers.VRCClampedLerp(_leftEarSmoothedVolume,
                    _leftEarIncomingVolume * Settings.Default.gain, 0.3f);
                _rightEarSmoothedVolume = Helpers.VRCClampedLerp(_rightEarSmoothedVolume,
                    _rightEarIncomingVolume * Settings.Default.gain, 0.3f);
                if (float.IsNaN(_leftEarSmoothedVolume) || float.IsNaN(_rightEarSmoothedVolume) ||
                    float.IsInfinity(_leftEarSmoothedVolume) || float.IsInfinity(_rightEarSmoothedVolume))
                {
                    // handle nan
                    _leftEarSmoothedVolume = 0;
                    _rightEarSmoothedVolume = 0;
                }

                _direction = Helpers.VRCClamp(-(_leftEarSmoothedVolume * 2) + (_rightEarSmoothedVolume * 2) + 0.5f);
                //log values with fixed decimal places
                OscParameter.SendAvatarParameter(Settings.Default.audio_direction, _direction);
                OscParameter.SendAvatarParameter(Settings.Default.audio_volume,
                    (_leftEarSmoothedVolume + _rightEarSmoothedVolume) / 2);
            }
            catch (Exception e)
            {
                // Often errors when trying to send a value while changing avatars
                Logger.Error(e, "Error sending OSC");
                SentrySdk.CaptureException(e);
                await Task.Delay(2000);
            }

            UpdateUi();
            await Task.Delay(25);
            _shouldUpdate = true;
        }
    }

    private void UpdateUi()
    {
        Helpers.mainWindow?.Dispatcher.InvokeAsync(new Action(() =>
            Helpers.mainWindow.LeftAudioMeter.Value = _leftEarSmoothedVolume));
        Helpers.mainWindow?.Dispatcher.InvokeAsync(new Action(() =>
            Helpers.mainWindow.RightAudioMeter.Value = _rightEarSmoothedVolume));
    }
}