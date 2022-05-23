using System;
using System.Windows.Controls;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace VRC_OSC_AudioEars;

class AudioEventListener : IMMNotificationClient
{
    private ComboBox? _comboBox;

    public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    {
        Helpers.mainWindow?.Dispatcher.Invoke(new Action(() => _comboBox = Helpers.mainWindow.DeviceName));
        Audio.Instance.UpdateUIDeviceList(_comboBox);
    }

    public void OnDeviceAdded(string pwstrDeviceId)
    {
        Helpers.mainWindow?.Dispatcher.Invoke(new Action(() => _comboBox = Helpers.mainWindow.DeviceName));
        Audio.Instance.UpdateUIDeviceList(_comboBox);
    }

    public void OnDeviceRemoved(string deviceId)
    {
        Helpers.mainWindow?.Dispatcher.Invoke(new Action(() => _comboBox = Helpers.mainWindow.DeviceName));
        Audio.Instance.UpdateUIDeviceList(_comboBox);
    }

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        Helpers.mainWindow?.Dispatcher.Invoke(new Action(() => _comboBox = Helpers.mainWindow.DeviceName));

        if (Audio.Instance.IsDefaultCurrent)
        {
            Audio.Instance.UpdateDefaultDevice(_comboBox);
            Audio.Instance.SetUpAudio((string)_comboBox?.SelectedItem!, _comboBox);
        }
    }

    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
    {
        //
    }
}