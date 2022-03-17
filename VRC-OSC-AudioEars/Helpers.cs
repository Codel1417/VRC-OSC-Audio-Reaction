namespace VRC_OSC_AudioEars;

public static class Helpers
{
    public static float Lerp(float firstFloat, float secondFloat, float by)
    {
        return firstFloat * (1 - by) + secondFloat * by;
    }
}