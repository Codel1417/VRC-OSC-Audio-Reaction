using System;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace VRC_OSC_AudioEars;

internal class AudioEventListener : IMMNotificationClient
{

    public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    {
        Audio.Queue.Enqueue(() => Audio.Instance.UpdateUiDeviceList());
    }

    public void OnDeviceAdded(string pwstrDeviceId)
    {
        Audio.Queue.Enqueue(() => Audio.Instance.UpdateUiDeviceList());
    }

    public void OnDeviceRemoved(string deviceId)
    {
        Audio.Queue.Enqueue(() => Audio.Instance.UpdateUiDeviceList());
    }

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        if (Audio.Instance.IsDefaultCurrent)
        {       
            string deviceName = "";
            Helpers.MainWindow?.Dispatcher.Invoke(new Action(() => deviceName = (string) Helpers.MainWindow.DeviceName.SelectedItem));
            Audio.Queue.Enqueue(() => Audio.Instance.UpdateUiDeviceList());
            Audio.Queue.Enqueue(() => Audio.Instance.SetUpAudio(deviceName));
        }
    }

    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
    {
        //
    }
}