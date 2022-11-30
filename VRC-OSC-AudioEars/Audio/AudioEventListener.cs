using System;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace VRC_OSC_AudioEars.Audio;

internal class AudioEventListener : IMMNotificationClient
{
    public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    {
        AudioReactionControl.UpdateUiDeviceList();
    }

    public void OnDeviceAdded(string pwstrDeviceId)
    {
        AudioReactionControl.UpdateUiDeviceList();
    }

    public void OnDeviceRemoved(string deviceId)
    {
        AudioReactionControl.UpdateUiDeviceList();
    }

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        string? deviceName = "";
        Helpers.MainWindow?.Dispatcher.Invoke(new Action(() =>
            deviceName = (string)Helpers.MainWindow.DeviceName.SelectedItem));
        AudioReactionControl.UpdateUiDeviceList();
        AudioReactionControl.SetUpAudio(deviceName);
    }

    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
    {
        //
    }
}