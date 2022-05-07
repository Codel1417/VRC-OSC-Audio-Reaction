﻿using BuildSoft.VRChat.Osc;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NLog;
using Sentry;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace VRC_OSC_AudioEars;

public class Audio
{
    private Audio() { }
    private static Audio instance = null;
    public static Audio Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new Audio();
            }
            return instance;
        }
    }

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
    public MMDevice? _activeDevice = null;
    private WasapiLoopbackCapture? _capture = null;
    private int _bytesPerSample = 0;
    public MMDeviceCollection? devices = null;
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
    public ComboBox UpdateUIDeviceList(ComboBox combobox)
    {
        Logger.Debug("Updating UI device list");
        combobox.Items.Clear();
        List<MMDevice> devices = GetDeviceList();
        MMDevice defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
        foreach (var wasapi in devices)
        {
            string name = wasapi.FriendlyName;
            //Logger.Debug("Device: " + name);
            combobox.Items.Add(name);
        }
        if (combobox.SelectedIndex == -1)
        {
            Logger.Debug("Setting default device: " + defaultDevice.FriendlyName);
            combobox.SelectedIndex = combobox.Items.IndexOf(defaultDevice.FriendlyName);
        }
        return combobox;
    }
    public List<MMDevice> GetDeviceList()
    {
        Logger.Info("Getting list of output devices");
        List<MMDevice> mMDevices = new();
        foreach (var wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
        {
            mMDevices.Add(wasapi);
        }
        return mMDevices;
    }
    public MMDevice? GetSelectedDevice(string selectedItem, ComboBox comboBox)
    {
        Logger.Info("Getting selected device: " + selectedItem);
        if (selectedItem != null )
        {
            foreach (MMDevice device in GetDeviceList())
            {
                if (device != null && device.FriendlyName.Equals(selectedItem) && device.State == DeviceState.Active)
                {
                    Logger.Debug("Found matching device: ", device.FriendlyName);
                    return device;
                }
            }
        }
        return null;
    }

    public Task SetUpAudio(String selectedItem, ComboBox comboBox)
    {
        MMDevice? newDevice = GetSelectedDevice(selectedItem, comboBox);
        try
        {
            Logger.Debug("Setting up audio");
            Logger.Trace("Getting audio device");
            if (newDevice != null && _activeDevice != null && _activeDevice.FriendlyName == newDevice.FriendlyName && _capture != null && _capture.CaptureState == CaptureState.Capturing)
            {
                return Task.CompletedTask;
            }


            if (_capture != null) _capture.StopRecording();
            if (_capture != null) _capture.Dispose();
            if (_activeDevice != null) _activeDevice.Dispose();
            _activeDevice = newDevice;
            if (_activeDevice != null)
            {
                Logger.Trace("Setting up Loopback capture");
                _capture = new WasapiLoopbackCapture(_activeDevice);
                Logger.Trace("Setting up Event listeners");
                _capture.DataAvailable += OnDataAvailable!;
                _bytesPerSample = _capture.WaveFormat.BitsPerSample / _capture.WaveFormat.BlockAlign;


                Logger.Debug("Configuring Sentry scope");
                Logger.Info("Device: " + _activeDevice.FriendlyName + " Bitrate: " + _capture.WaveFormat.BitsPerSample + " SampleRate: " + _capture.WaveFormat.SampleRate + " Dataflow: " + _activeDevice.DataFlow);
                SentrySdk.ConfigureScope(scope => scope.Contexts["Audio Device"] = new
                {
                    _activeDevice.FriendlyName,
                    _capture.WaveFormat.BitsPerSample,
                    _capture.WaveFormat.SampleRate,
                    _capture.WaveFormat.Channels,
                    Encoding = _capture.WaveFormat.Encoding.ToString(),
                    CaptureState = _capture.CaptureState.ToString(),
                    ShareMode = _capture.ShareMode.ToString(),
                    _capture.WaveFormat.BlockAlign,
                    _capture.WaveFormat.AverageBytesPerSecond,
                    DeviceState = _activeDevice.State.ToString(),
                    _activeDevice.DataFlow,
                });;

                Logger.Trace("Starting capture");
                _capture.StartRecording();

                SentrySdk.AddBreadcrumb(
                    message: "Audio Set Up " + _activeDevice.FriendlyName,
                    category: "Audio",
                    level: BreadcrumbLevel.Info);

                OscUtility.SendPort = Settings.Default.port;
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            SentrySdk.CaptureException(ex);
            if (Helpers.mainWindow != null && Helpers.mainWindow.SnackBar.MessageQueue != null)
            {
                Helpers.mainWindow?.Dispatcher.InvokeAsync(new Action(() => Helpers.mainWindow.SnackBar.MessageQueue.Enqueue("Unable to use selected device!")));
            }
            Settings.Default.enabled = false;
            return Task.CompletedTask;
        }
    }

    public async Task Update()
    {
        while (true)
        {
            if (Settings.Default.enabled == false)
            {
                await Task.Delay(25);
                continue;
            }

            try
            {
                _leftEarSmoothedVolume = Helpers.VRCClampedLerp(_leftEarSmoothedVolume, _leftEarIncomingVolume * Settings.Default.gain, 0.3f);
                _rightEarSmoothedVolume = Helpers.VRCClampedLerp(_rightEarSmoothedVolume, _rightEarIncomingVolume * Settings.Default.gain, 0.3f);
                if (float.IsNaN(_leftEarSmoothedVolume) || float.IsNaN(_rightEarSmoothedVolume) || float.IsInfinity(_leftEarSmoothedVolume) || float.IsInfinity(_rightEarSmoothedVolume))
                {
                    // handle nan
                    _leftEarSmoothedVolume = 0;
                    _rightEarSmoothedVolume = 0;
                }
                _direction = Helpers.VRCClamp(-(_leftEarSmoothedVolume * 2) + (_rightEarSmoothedVolume * 2) + 0.5f);
                //log values with fixed decimal places
                //Logger.Trace($"Left Ear: {_leftEarSmoothedVolume:F3} Right Ear: {_rightEarSmoothedVolume:F3} Direction: {_direction:F3}");
                OscParameter.SendAvatarParameter(Settings.Default.audio_direction, _direction); ;
                OscParameter.SendAvatarParameter(Settings.Default.audio_volume, (_leftEarSmoothedVolume + _rightEarSmoothedVolume) / 2);
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
        Helpers.mainWindow?.Dispatcher.InvokeAsync(new Action(() => Helpers.mainWindow.LeftAudioMeter.Value = _leftEarSmoothedVolume));
        Helpers.mainWindow?.Dispatcher.InvokeAsync(new Action(() => Helpers.mainWindow.RightAudioMeter.Value = _rightEarSmoothedVolume));

    }
}