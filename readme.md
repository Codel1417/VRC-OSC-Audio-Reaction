﻿# VRC OSC Audio Input

This application takes the audio from your default audio output device and then sends the OSC parameters for direction and amplitude.

## Video
[![Video Demo](http://img.youtube.com/vi/nRYSYIVfluI/0.jpg)](http://www.youtube.com/watch?v=nRYSYIVfluI)


## How to Use

1) Add the synced float parameters `audio_volume` and `audio_direction` to your VRChat avatar.
2) Enable OSC in VRChat inside the Action Menu.
3) Launch This Program.

### Parameters

Two `float` parameters are available:

1) `audio_direction` at `/avatar/parameters/audio_direction`: the direction of the sound. Where 0.5 is centered, 0 is left 1 is right.
2) `audio_volume` at `/avatar/parameters/audio_volume`: the volume of the sound. Where 0 is silent, 1 is loud. This is based on the Windows Audio API.

Feel free to suggest other parameters.

### Optional Command Line Arguments

- `--sentry` | Disables the use of Sentry for error reporting.
- `--verbose` | Enables verbose logging.
- `--version` | Displays the version of this program.
- `--help` | Displays this help message.

## Sentry Reporting

### This application reports crashes to [Sentry](https://sentry.io/). Data collected during a crash includes

- The version of this program.
- The audio device info including the name, bit depth, and sample rate.
- OS Version.
- Logged Messages.
- Stack Traces if available.

All data is anonymous and no personal information is collected. 
Data is only sent in the event of an error 
To opt out of this reporting, run this program with the `--sentry` flag.

## License

This program is licensed under the [MIT license](LICENSE).

## Future Plans

* A graphical interface instead of command line.