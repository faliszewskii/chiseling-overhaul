namespace ChiselingOverhaul.Utils;

public static class HarmonyUtils
{
    public static bool ShortCircuitReturn<T>(ref T result, T value)
    {
        result = value;
        return false;
    }
    public static bool ShortCircuitVoid()
    {
        return false;
    }
}