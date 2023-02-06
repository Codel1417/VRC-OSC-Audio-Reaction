namespace VRC_OSC_AudioEars.Audio;

public static class AudioReactionControl
{
    public static void SetUpAudio() => Audio.Queue.Enqueue(() =>  Audio.Instance.SetUpAudio());

    /// <summary>
    /// Starts audio processing and OSC Messages
    /// </summary>
    public static void Enable() => Audio.Queue.Enqueue(() => Audio.Instance.Enable());

    /// <summary>
    /// Pauses audio and OSC messages
    /// </summary>
    public static void Disable() => Audio.Queue.Enqueue(() => Audio.Instance.Disable());

    /// <summary>
    /// Stops the event loop
    /// </summary>
    public static void Stop() => Audio.Queue.Enqueue(() => Audio.Instance.StopLoop());

    /// <summary>
    /// Starts the Event loop
    /// </summary>
    public static void Start() => Audio.StartLoop();

    public static void UpdateGain(float gain)
    {
        Audio.Queue.Enqueue(() => Audio.Instance.SetGain(gain));
    }
}
