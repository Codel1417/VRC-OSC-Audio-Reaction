# VRC OSC Audio Input

This take the audio from your default audio output device and sends OSC parameters for direction and amplitude.

### Parameters

Two `float` parameters are available:

1) `audio_direction` at `/avatar/parameters/audio_direction`: the direction of the sound. Where 0.5 is centered, 0 is left 1 is right.
2) `audio_volume` at `/avatar/parameters/audio_volume`: the volume of the sound. Where 0 is silent, 1 is loud. This is based on the Windows Audio API

## How to use

1) Add the synced float parameters `audio_volume` and `audio_direction` to your VRChat avatar.
2) Enable OSC in VRChat inside the Action Menu.
3) Launch This Program.

## Credits

* [NAudio](https://github.com/naudio/NAudio)
* [VRCOscLib](https://github.com/ChanyaVRC/VRCOscLib)
