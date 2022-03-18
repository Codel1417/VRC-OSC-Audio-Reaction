using BuildSoft.VRChat.Osc;
using BuildSoft.VRChat.Osc.Avatar;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using VRC_OSC_AudioEars;


const string audioVolumeParameter = "audio_volume";
const string audioDirectionParameter = "audio_direction";

Console.WriteLine("Starting...");

// Get Default audio device
MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
MMDevice device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
DirectSoundOut outputDevice = new DirectSoundOut(DirectSoundOut.DSDEVID_DefaultPlayback); // Fixes virtual audio devices by existing?


Console.WriteLine("Device: " + device.FriendlyName);

float leftEarSmoothedVolume = 0;
float rightEarSmoothedVolume = 0;
float masterSmoothedVolume = 0;
float leftEarIncomingVolume = 0;
float rightEarIncomingVolume = 0;
float masterIncomingVolume = 0;
float direction = 0;

// ReSharper disable once NotAccessedVariable
OscAvatarConfig config = await OscAvatarConfig.WaitAndCreateAtCurrentAsync();
Console.WriteLine("Started");


while (true)
{
    try
    {
        config = await OscAvatarConfig.WaitAndCreateAtCurrentAsync();

        leftEarIncomingVolume = device.AudioMeterInformation.PeakValues[0];
        rightEarIncomingVolume = device.AudioMeterInformation.PeakValues[1];
        masterIncomingVolume = device.AudioMeterInformation.MasterPeakValue;

        leftEarSmoothedVolume = Helpers.Lerp(leftEarSmoothedVolume, leftEarIncomingVolume, 0.3f);
        rightEarSmoothedVolume = Helpers.Lerp(rightEarSmoothedVolume, rightEarIncomingVolume, 0.3f);
        masterSmoothedVolume = Helpers.Lerp(masterSmoothedVolume, masterIncomingVolume, 0.3f);

        direction = Math.Clamp(-leftEarSmoothedVolume + rightEarSmoothedVolume + 0.5f, 0, 1);
        //Write all values to the console
        Console.WriteLine("Left Ear: " + leftEarSmoothedVolume + " Right Ear: " + rightEarSmoothedVolume + " Master: " + masterSmoothedVolume + " Direction: " + direction);
        
        OscParameter.SendValue("/avatar/parameters/" + audioDirectionParameter, direction);

        OscParameter.SendValue("/avatar/parameters/" + audioVolumeParameter, masterSmoothedVolume);
    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
        await Task.Delay(2000);

    }


    await Task.Delay(10);
}