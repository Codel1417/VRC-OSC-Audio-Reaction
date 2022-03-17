
using BuildSoft.VRChat.Osc;
using BuildSoft.VRChat.Osc.Avatar;
using NAudio.CoreAudioApi;
using VRC_OSC_AudioEars;


const string audioVolumeParameter = "audio_volume";
const string audioDirectionParameter = "audio_direction";

// Get Default audio device
MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
MMDevice device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
Console.Write("Device: " + device.
    FriendlyName);

float leftEarSmoothedVolume = 0;
float rightEarSmoothedVolume = 0;
float masterSmoothedVolume = 0;

while (true)
{
    OscAvatarConfig config = await OscAvatarConfig.WaitAndCreateAtCurrentAsync();
    if (config.Parameters.ContainsKey(audioDirectionParameter) |
        config.Parameters.ContainsKey(audioVolumeParameter))
    {

        if (device.State == DeviceState.Active && device.AudioMeterInformation.PeakValues.Count > 1)
        {
            float leftEarIncomingVolume = device.AudioMeterInformation.PeakValues[0];
            float rightEarIncomingVolume = device.AudioMeterInformation.PeakValues[1];
            float masterIncomingVolume = device.AudioMeterInformation.MasterPeakValue;
        
            leftEarSmoothedVolume = Helpers.Lerp(leftEarSmoothedVolume, leftEarIncomingVolume, 0.3f);
            rightEarSmoothedVolume = Helpers.Lerp(rightEarSmoothedVolume, rightEarIncomingVolume, 0.3f);
            masterSmoothedVolume = Helpers.Lerp(masterSmoothedVolume, masterIncomingVolume, 0.3f);
            
            if (config.Parameters.ContainsKey(audioDirectionParameter))
            {
                float direction = Math.Clamp(-leftEarSmoothedVolume + rightEarSmoothedVolume + 0.5f , 0, 1);
                OscParameter.SendValue("/avatar/parameters/" + audioDirectionParameter, direction);
            }
            if (config.Parameters.ContainsKey(audioVolumeParameter))
            {
                OscParameter.SendValue("/avatar/parameters/" + audioVolumeParameter, masterSmoothedVolume);
            }
        
        }
        else
        {
            device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            Console.Write("Device: " + device.FriendlyName);
            await Task.Delay(1000);
        }
    }
    else
    {
        await Task.Delay(1000);
    }
    await Task.Delay(10);
}

